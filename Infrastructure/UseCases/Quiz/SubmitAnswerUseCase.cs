using Application;
using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Domain.Model.User;
using Infrastructure.Contracts.Quiz.Requests;
using Infrastructure.Contracts.Quiz.Responses;
using Infrastructure.Contracts.Flows.Responses;
using Microsoft.EntityFrameworkCore;
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

        // Record answer
        var answer = UserAnswer.Create(
            sessionId,
            request.NodeId,
            node.AttributeKey ?? "answer",
            request.Value
        );
        await _answers.AddAsync(answer, ct);

        // Find next node by evaluating edges
        var edges = await _db.Edges
            .Where(e => e.SourceNodeId == session.CurrentNodeId && e.FlowId == session.FlowId)
            .OrderByDescending(e => e.Priority)
            .ToListAsync(ct);

        Edge? matchingEdge = null;
        foreach (var edge in edges)
        {
            if (string.IsNullOrEmpty(edge.ConditionsJson))
            {
                // Unconditional match
                matchingEdge = edge;
                break;
            }

            // Parse and evaluate conditions
            try
            {
                var conditions = JsonSerializer.Deserialize<List<EdgeCondition>>(edge.ConditionsJson);
                if (conditions != null && EvaluateConditions(conditions, node.AttributeKey ?? "answer", request.Value))
                {
                    matchingEdge = edge;
                    break;
                }
            }
            catch
            {
                // JSON parse error, skip this edge
                continue;
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
            var completedResponse = new SessionStateResponse(
                SessionId: session.Id,
                FlowId: session.FlowId,
                Status: session.Status.ToString(),
                StartedAt: session.StartedAt,
                CompletedAt: session.CompletedAt,
                CurrentNode: null
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

    private bool EvaluateConditions(List<EdgeCondition> conditions, string attributeKey, string value)
    {
        // All conditions must match
        foreach (var condition in conditions)
        {
            if (condition.AttributeKey != attributeKey || condition.Value != value)
                return false;
        }
        return true;
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
                .Select(x => new QuizOfferResponse(x.o.Id, x.o.Name, x.o.Slug, x.o.Price, x.o.ImageUrl, x.o.CtaText, x.o.CtaUrl, x.no.IsPrimary))
                .ToList()
        );
    }
}

internal record EdgeCondition(string AttributeKey, string Value);
