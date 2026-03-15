using Application;
using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Domain.Model.User;
using Infrastructure.Contracts.Quiz.Requests;
using Infrastructure.Contracts.Quiz.Responses;
using Infrastructure.Contracts.Flows.Responses;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;

namespace Infrastructure.UseCases.Quiz;

/// <summary>
/// Use case: Submit an answer to the current quiz node.
///
/// Logic:
/// 1. Load session, verify it's InProgress
/// 2. Verify the submitted node matches current node
/// 3. Record the answer
/// 4. Find next node by evaluating edges (with conditions)
/// 5. Move to next node or complete the session
/// 6. Return updated session state
/// </summary>
public sealed class SubmitAnswerUseCase
{
    private readonly IUserSessionRepository _sessions;
    private readonly IUserAnswerRepository _answers;
    private readonly IUnitOfWork _uow;
    private readonly AppDbContext _db;

    public SubmitAnswerUseCase(
        IUserSessionRepository sessions,
        IUserAnswerRepository answers,
        IUnitOfWork uow,
        AppDbContext db)
    {
        _sessions = sessions;
        _answers = answers;
        _uow = uow;
        _db = db;
    }

    public async Task<FlowResult<SessionStateResponse>> ExecuteAsync(
        Guid sessionId,
        SubmitAnswerRequest request,
        CancellationToken ct = default)
    {
        // Load session
        var session = await _sessions.GetByIdAsync(sessionId, ct);
        if (session is null)
            return FlowResult<SessionStateResponse>.NotFound("Session not found.");

        // Verify session is in progress
        if (session.Status != SessionStatus.InProgress)
            return FlowResult<SessionStateResponse>.Fail("Session is not active.", 422);

        // Verify node matches current node
        if (request.NodeId != session.CurrentNodeId)
            return FlowResult<SessionStateResponse>.Fail("Node mismatch.", 422);

        // Load current node to get attribute key
        var node = await _db.Nodes.FirstOrDefaultAsync(n => n.Id == session.CurrentNodeId, ct);
        if (node is null)
            return FlowResult<SessionStateResponse>.NotFound("Node not found.");

        // Record answer only for Question nodes
        if (node.Type == NodeType.Question && !string.IsNullOrWhiteSpace(request.Value))
        {
            var answer = UserAnswer.Create(
                sessionId,
                request.NodeId,
                node.AttributeKey ?? "answer",
                request.Value
            );
            await _answers.AddAsync(answer, ct);
        }

        // Build full answer context: all previous answers + current answer
        var previousAnswers = await _db.UserAnswers
            .Where(a => a.SessionId == sessionId)
            .ToListAsync(ct);

        var answerContext = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in previousAnswers)
            answerContext[a.AttributeKey] = a.Value;

        // Include the current answer (may not be saved yet if it's a non-Question node)
        if (!string.IsNullOrWhiteSpace(node.AttributeKey) && !string.IsNullOrWhiteSpace(request.Value))
            answerContext[node.AttributeKey] = request.Value;

        // Find next node by evaluating edges
        var edges = await _db.Edges
            .Where(e => e.SourceNodeId == session.CurrentNodeId && e.FlowId == session.FlowId)
            .OrderByDescending(e => e.Priority)
            .ToListAsync(ct);

        Edge? matchingEdge = null;
        foreach (var edge in edges)
        {
            if (string.IsNullOrWhiteSpace(edge.ConditionsJson))
            {
                // Unconditional edge — always matches
                matchingEdge = edge;
                break;
            }

            if (await EvaluateConditionsAsync(edge.ConditionsJson, answerContext, ct))
            {
                matchingEdge = edge;
                break;
            }
        }

        // Move to next node or complete
        if (matchingEdge != null)
        {
            session.MoveToNode(matchingEdge.TargetNodeId);
        }
        else
        {
            session.Complete();
        }

        // Save changes
        _sessions.Update(session);
        await _uow.SaveChangesAsync();

        // Return updated state
        if (session.Status == SessionStatus.Completed)
        {
            var completedNode = await BuildCurrentNodeAsync(session.CurrentNodeId, ct);
            var completedResponse = new SessionStateResponse(
                SessionId: session.Id,
                FlowId: session.FlowId,
                Status: session.Status.ToString(),
                StartedAt: session.StartedAt,
                CompletedAt: session.CompletedAt,
                CurrentNode: completedNode
            );
            return FlowResult<SessionStateResponse>.Ok(completedResponse);
        }

        // Load new current node
        var currentNode = await BuildCurrentNodeAsync(session.CurrentNodeId, ct);
        if (currentNode is null)
            return FlowResult<SessionStateResponse>.NotFound("Current node not found.");

        var response = new SessionStateResponse(
            SessionId: session.Id,
            FlowId: session.FlowId,
            Status: session.Status.ToString(),
            StartedAt: session.StartedAt,
            CompletedAt: session.CompletedAt,
            CurrentNode: currentNode
        );

        return FlowResult<SessionStateResponse>.Ok(response);
    }

    /// <summary>
    /// Evaluates edge conditions against the full session answer context.
    ///
    /// Supports two stored formats:
    ///   Array  : [{"AttributeKey":"goal","Operator":"eq","Value":"option_1"},
    ///             {"AttributeKey":"age","Operator":"between","Value":"0","ValueTo":"40"}]
    ///   Object : {"operator":"AND","rules":[{"attribute":"goal","op":"eq","value":"option_2"}]}
    ///
    /// All conditions must pass (implicit AND).
    /// </summary>
    private async Task<bool> EvaluateConditionsAsync(
        string conditionsJson,
        Dictionary<string, string> answerContext,
        CancellationToken ct)
    {
        var trimmed = conditionsJson.Trim();
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        if (trimmed.StartsWith('['))
        {
            var conditions = JsonSerializer.Deserialize<List<OldEdgeCondition>>(trimmed, jsonOptions);
            if (conditions == null || conditions.Count == 0) return true;

            foreach (var c in conditions)
            {
                if (string.IsNullOrWhiteSpace(c.AttributeKey))
                    return false;

                if (!answerContext.TryGetValue(c.AttributeKey, out var storedValue))
                    return false;

                if (!await EvalOperatorAsync(c.Operator ?? "eq", storedValue, c.Value, c.ValueTo, ct))
                    return false;
            }
            return true;
        }

        if (trimmed.StartsWith('{'))
        {
            var wrapper = JsonSerializer.Deserialize<NewEdgeConditionWrapper>(trimmed, jsonOptions);
            if (wrapper?.Rules == null || wrapper.Rules.Count == 0) return true;

            foreach (var r in wrapper.Rules)
            {
                if (string.IsNullOrWhiteSpace(r.Attribute))
                    return false;

                if (!answerContext.TryGetValue(r.Attribute, out var storedValue))
                    return false;

                if (!await EvalOperatorAsync(r.Op, storedValue, r.Value, null, ct))
                    return false;
            }
            return true;
        }

        return true;
    }

    private async Task<bool> EvalOperatorAsync(
        string op, string storedValue, string condValue, string? condValueTo, CancellationToken ct)
    {
        switch (op.ToLowerInvariant())
        {
            case "eq":      return string.Equals(storedValue, condValue, StringComparison.OrdinalIgnoreCase);
            case "neq":     return !string.Equals(storedValue, condValue, StringComparison.OrdinalIgnoreCase);
            case "in":      return condValue.Split(',', StringSplitOptions.TrimEntries)
                                            .Contains(storedValue, StringComparer.OrdinalIgnoreCase);
            case "between": return await EvalBetweenAsync(storedValue, condValue, condValueTo, ct);
            case "gt":      return await EvalNumericAsync(storedValue, ct) is { } v1 &&
                                   decimal.TryParse(condValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var gt) &&
                                   v1 > gt;
            case "gte":     return await EvalNumericAsync(storedValue, ct) is { } v2 &&
                                   decimal.TryParse(condValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var gte) &&
                                   v2 >= gte;
            case "lt":      return await EvalNumericAsync(storedValue, ct) is { } v3 &&
                                   decimal.TryParse(condValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var lt) &&
                                   v3 < lt;
            case "lte":     return await EvalNumericAsync(storedValue, ct) is { } v4 &&
                                   decimal.TryParse(condValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var lte) &&
                                   v4 <= lte;
            default:        return false;
        }
    }

    private async Task<bool> EvalBetweenAsync(
        string storedValue, string lo, string? hi, CancellationToken ct)
    {
        if (!decimal.TryParse(lo, NumberStyles.Any, CultureInfo.InvariantCulture, out var loVal)) return false;
        if (!decimal.TryParse(hi, NumberStyles.Any, CultureInfo.InvariantCulture, out var hiVal)) return false;
        var num = await EvalNumericAsync(storedValue, ct);
        return num is { } v && v >= loVal && v <= hiVal;
    }

    /// <summary>
    /// Resolves a stored answer value to a decimal for numeric comparisons.
    /// If the value is already numeric, returns it directly.
    /// Otherwise looks up the Option by value and parses its label as a number.
    /// </summary>
    private async Task<decimal?> EvalNumericAsync(string storedValue, CancellationToken ct)
    {
        if (decimal.TryParse(storedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var direct))
            return direct;

        var option = await _db.Options.FirstOrDefaultAsync(o => o.Value == storedValue, ct);
        if (option == null) return null;

        if (decimal.TryParse(option.Label, NumberStyles.Any, CultureInfo.InvariantCulture, out var labelNum))
            return labelNum;

        return null;
    }

    private async Task<CurrentNodeResponse?> BuildCurrentNodeAsync(Guid nodeId, CancellationToken ct)
    {
        var node = await _db.Nodes.Include(n => n.Options).FirstOrDefaultAsync(n => n.Id == nodeId, ct);
        if (node is null)
            return null;

        var nodeOfferData = await _db.NodeOffers
            .Where(no => no.NodeId == nodeId)
            .Join(_db.Offers, no => no.OfferId, o => o.Id, (no, o) => new { no, o })
            .ToListAsync(ct);

        return new CurrentNodeResponse(
            Id: node.Id,
            Type: node.Type.ToString(),
            AttributeKey: node.AttributeKey,
            Title: node.Title,
            Description: node.Description,
            MediaUrl: node.MediaUrl,
            Options: node.Options.OrderBy(o => o.DisplayOrder)
                .Select(o => new QuizOptionResponse(o.Id, o.Label, o.Value, o.DisplayOrder, o.MediaUrl))
                .ToList(),
            Offers: nodeOfferData
                .Select(x => new QuizOfferResponse(
                    x.o.Id, x.o.Name, x.o.Slug,
                    x.o.Description, x.o.Duration, x.o.DigitalContent,
                    x.o.PhysicalWellnessKitName, x.o.PhysicalWellnessKitItems,
                    x.o.Price, x.o.ImageUrl, x.o.CtaText, x.o.CtaUrl,
                    x.no.IsPrimary))
                .ToList()
        );
    }
}

// Old array format: [{"AttributeKey":"age","Operator":"between","Value":"0","ValueTo":"40"}]
internal record OldEdgeCondition(string AttributeKey, string? Operator, string Value, string? ValueTo);

// New object format: {"operator":"AND","rules":[{"attribute":"goal","op":"eq","value":"option_2"}]}
internal record NewEdgeRule(string Attribute, string Op, string Value);
internal record NewEdgeConditionWrapper(string Operator, List<NewEdgeRule> Rules);
