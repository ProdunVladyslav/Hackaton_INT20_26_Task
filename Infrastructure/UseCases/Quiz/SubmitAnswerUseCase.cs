using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Domain.Model.User;
using Infrastructure.Contracts.Quiz.Requests;
using Infrastructure.Contracts.Quiz.Responses;
using Infrastructure.Contracts.Flows.Responses;
using System.Globalization;
using System.Text.Json;

namespace Infrastructure.UseCases.Quiz;

public sealed class SubmitAnswerUseCase(
    IUserSessionRepository _sessions,
    IUserAnswerRepository _answers,
    IEdgeRepository _edges,
    INodeRepository _nodes,
    INodeOfferRepository _nodeOffers,
    ILeadRepository _leads,
    IFlowRepository _flows,
    ISessionOfferRepository _sessionOffers,
    IUnitOfWork _uow)
{
    public async Task<FlowResult<SessionStateResponse>> ExecuteAsync(
        Guid sessionId,
        SubmitAnswerRequest request,
        CancellationToken ct = default)
    {
        var session = await _sessions.GetByIdAsync(sessionId, ct);
        if (session is null)
            return FlowResult<SessionStateResponse>.NotFound("Session not found.");

        if (session.Status != SessionStatus.InProgress)
            return FlowResult<SessionStateResponse>.Fail("Session is not active.", 422);

        if (request.NodeId != session.CurrentNodeId)
            return FlowResult<SessionStateResponse>.Fail("Node mismatch.", 422);

        var node = await _nodes.GetByIdAsync(session.CurrentNodeId!.Value, ct);
        if (node is null)
            return FlowResult<SessionStateResponse>.NotFound("Node not found.");

        var lastAnsweredAt = await _answers.GetLastAnsweredAtAsync(sessionId) ?? session.StartedAt;

        // ── Record answer ─────────────────────────────────────────────────────
        if (node.Type == NodeType.Question && !string.IsNullOrWhiteSpace(request.Value))
        {
            var answer = UserAnswer.Create(
                sessionId,
                request.NodeId,
                node.AttributeKey ?? "answer",
                request.Value,
                lastAnsweredAt);
            await _answers.AddAsync(answer, ct); 

            // Accumulate score for choice-based questions
            if (node.AnswerType is AnswerType.SingleChoice or AnswerType.MultipleChoice)
            {
                var selectedValues = request.Value
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                var delta = node.Options
                    .Where(o => selectedValues.Contains(o.Value, StringComparer.OrdinalIgnoreCase))
                    .Sum(o => o.ScoreDelta);

                session.AddScore(delta);
            }
        }

        var NodeTypee = node.Type;
        var NodeTypeLeadCaptture = node.Type == NodeType.LeadCapture;
        var NodeLeadCapt = node.LeadCapture;
        var NodeLeadFields = request.LeadFields;

        // LeadCapture — store each field as a UserAnswer with its reserved key
        if (node.Type == NodeType.LeadCapture
            && node.LeadCapture is not null
            && request.LeadFields is not null)
        {
            foreach (var field in node.LeadCapture.Fields)
            {
                if (request.LeadFields.TryGetValue(field.AttributeKey, out var fieldValue)
                    && !string.IsNullOrWhiteSpace(fieldValue))
                {
                    var answer = UserAnswer.Create(
                        sessionId,
                        request.NodeId,
                        field.AttributeKey,
                        fieldValue,
                        lastAnsweredAt);
                    await _answers.AddAsync(answer, ct);
                }
                else if (field.IsRequired)
                {
                    return FlowResult<SessionStateResponse>.Fail(
                        $"Required field '{field.FieldType}' is missing.", 422);
                }
            }
        }

        // ── Build answer context for edge evaluation ──────────────────────────
        var previousAnswers = await _answers.GetBySessionAsync(sessionId, ct);
        var answerContext = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var a in previousAnswers)
            answerContext[a.AttributeKey] = a.Value;

        if (!string.IsNullOrWhiteSpace(node.AttributeKey) && !string.IsNullOrWhiteSpace(request.Value))
            answerContext[node.AttributeKey] = request.Value;

        // Inject score so edges can route on __score__ >= threshold
        answerContext["__score__"] = session.Score.ToString();

        // Inject LeadCapture fields from the current request — they are queued
        // in the EF tracker but not yet persisted, so the DB query above misses them.
        if (node.Type == NodeType.LeadCapture && request.LeadFields is not null)
        {
            foreach (var (key, value) in request.LeadFields)
                if (!string.IsNullOrWhiteSpace(value))
                    answerContext[key] = value;
        }

        // ── Evaluate edges ────────────────────────────────────────────────────
        var edges = await _edges.GetBySourceNodeAsync(session.CurrentNodeId!.Value, session.FlowId, ct);

        Edge? matchingEdge = null;
        foreach (var edge in edges)
        {
            if (string.IsNullOrWhiteSpace(edge.ConditionsJson))
            {
                matchingEdge = edge;
                break;
            }

            if (await EvaluateConditionsAsync(edge.ConditionsJson, answerContext, ct))
            {
                matchingEdge = edge;
                break;
            }
        }

        if (matchingEdge is not null)
        {
            session.MoveToNode(matchingEdge.TargetNodeId);

            var hasOutgoing = await _edges.HasOutgoingEdgesAsync(
                matchingEdge.TargetNodeId, session.FlowId, ct);

            if (!hasOutgoing)
            {
                session.Complete();
                await TrackOfferImpressionsAsync(session, matchingEdge.TargetNodeId, ct);

                // ── Lead creation ─────────────────────────────────────────────
                await TryCreateLeadAsync(session, matchingEdge.TargetNodeId, answerContext, ct);
            }
        }
        else
        {
            session.Complete();
        }

        _sessions.Update(session);
        await _uow.SaveChangesAsync(ct);

        var currentNode = session.CurrentNodeId.HasValue
            ? await BuildCurrentNodeAsync(session.CurrentNodeId.Value, ct)
            : null;

        return FlowResult<SessionStateResponse>.Ok(new SessionStateResponse(
            SessionId: session.Id,
            FlowId: session.FlowId,
            Status: session.Status.ToString(),
            StartedAt: session.StartedAt,
            CompletedAt: session.CompletedAt,
            CurrentNode: currentNode
        ));
    }

    // ── Offer impression tracking ─────────────────────────────────────────────

    private async Task TrackOfferImpressionsAsync(
        UserSession session, Guid nodeId, CancellationToken ct)
    {
        var nodeOffers = await _nodeOffers.GetByNodeIdAsync(nodeId, ct);
        foreach (var no in nodeOffers)
        {
            var existing = await _sessionOffers.GetBySessionAndOfferAsync(
                session.Id, no.OfferId, ct);

            if (existing is null)
                await _sessionOffers.AddAsync(
                    SessionOffer.Create(session.Id, no.OfferId, no.IsPrimary), ct);
        }
    }

    // ── Edge condition evaluation ─────────────────────────────────────────────

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
            if (conditions is null || conditions.Count == 0) return true;

            foreach (var c in conditions)
            {
                if (string.IsNullOrWhiteSpace(c.AttributeKey)) return false;
                if (!answerContext.TryGetValue(c.AttributeKey, out var storedValue)) return false;
                if (!await EvalOperatorAsync(c.Operator ?? "eq", storedValue, c.Value, c.ValueTo, ct))
                    return false;
            }
            return true;
        }

        if (trimmed.StartsWith('{'))
        {
            var wrapper = JsonSerializer.Deserialize<NewEdgeConditionWrapper>(trimmed, jsonOptions);
            if (wrapper?.Rules is null || wrapper.Rules.Count == 0) return true;

            var isOr = string.Equals(wrapper.Operator, "OR", StringComparison.OrdinalIgnoreCase);

            if (isOr)
            {
                foreach (var r in wrapper.Rules)
                {
                    if (string.IsNullOrWhiteSpace(r.AttributeKey)) continue;
                    if (!answerContext.TryGetValue(r.AttributeKey, out var storedValue)) continue;
                    if (await EvalOperatorAsync(r.Operator, storedValue, r.Value, r.ValueTo, ct))
                        return true;
                }
                return false;
            }
            else
            {
                foreach (var r in wrapper.Rules)
                {
                    if (string.IsNullOrWhiteSpace(r.AttributeKey)) return false;
                    if (!answerContext.TryGetValue(r.AttributeKey, out var storedValue)) return false;
                    if (!await EvalOperatorAsync(r.Operator, storedValue, r.Value, r.ValueTo, ct))
                        return false;
                }
                return true;
            }
        }

        return true;
    }

    private async Task<bool> EvalOperatorAsync(
        string op, string storedValue, string condValue, string? condValueTo, CancellationToken ct)
    {
        switch (op.ToLowerInvariant())
        {
            case "eq": return string.Equals(storedValue, condValue, StringComparison.OrdinalIgnoreCase);
            case "neq": return !string.Equals(storedValue, condValue, StringComparison.OrdinalIgnoreCase);
            case "in":
                return condValue
                    .Split(',', StringSplitOptions.TrimEntries)
                    .Contains(storedValue, StringComparer.OrdinalIgnoreCase);
            case "between": return await EvalBetweenAsync(storedValue, condValue, condValueTo, ct);
            case "gt":
                return await _answers.ResolveNumericValueAsync(storedValue, ct) is { } v1 &&
                    decimal.TryParse(condValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var gt)
                    && v1 > gt;
            case "gte":
                return await _answers.ResolveNumericValueAsync(storedValue, ct) is { } v2 &&
                    decimal.TryParse(condValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var gte)
                    && v2 >= gte;
            case "lt":
                return await _answers.ResolveNumericValueAsync(storedValue, ct) is { } v3 &&
                    decimal.TryParse(condValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var lt)
                    && v3 < lt;
            case "lte":
                return await _answers.ResolveNumericValueAsync(storedValue, ct) is { } v4 &&
                    decimal.TryParse(condValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var lte)
                    && v4 <= lte;
            default: return false;
        }
    }

    private async Task<bool> EvalBetweenAsync(
        string storedValue, string lo, string? hi, CancellationToken ct)
    {
        if (!decimal.TryParse(lo, NumberStyles.Any, CultureInfo.InvariantCulture, out var loVal)) return false;
        if (!decimal.TryParse(hi, NumberStyles.Any, CultureInfo.InvariantCulture, out var hiVal)) return false;
        var num = await _answers.ResolveNumericValueAsync(storedValue, ct);
        return num is { } v && v >= loVal && v <= hiVal;
    }

    // ── Response builder ──────────────────────────────────────────────────────

    private async Task<CurrentNodeResponse?> BuildCurrentNodeAsync(Guid nodeId, CancellationToken ct)
    {
        var node = await _nodes.GetWithOptionsAsync(nodeId, ct);
        if (node is null) return null;

        var nodeOfferData = await _nodeOffers.GetByNodeIdWithOfferForQuizAsync(nodeId, ct);

        var options = node.Options
            .OrderBy(o => o.DisplayOrder)
            .Select(o => new QuizOptionResponse(
                o.Id, o.Label, o.Value, o.DisplayOrder, o.MediaUrl, o.ScoreDelta))
            .ToList();

        var offers = nodeOfferData
            .Select(x => new QuizOfferResponse(
                Id: x.Offer.Id,
                Name: x.Offer.Name,
                Slug: x.Offer.Slug,
                Headline: x.Offer.Headline,
                Body: x.Offer.Body,
                ImageUrl: x.Offer.ImageUrl,
                CalendarUrl: x.Offer.CalendarUrl,
                CalendarProvider: x.Link.CalendarProvider?.ToString(),
                CtaText: x.Offer.CtaText,
                CtaUrl: x.Offer.CtaUrl,
                IsPrimary: x.Link.IsPrimary,
                Tier: x.Link.Tier.ToString()))
            .ToList();

        QuizLeadCaptureResponse? leadCapture = null;
        if (node.LeadCapture is not null)
        {
            leadCapture = new QuizLeadCaptureResponse(
                IsRequired: node.LeadCapture.IsRequired,
                Fields: node.LeadCapture.Fields
                    .OrderBy(f => f.DisplayOrder)
                    .Select(f => new QuizLeadCaptureFieldResponse(
                        FieldType: f.FieldType.ToString(),
                        AttributeKey: f.AttributeKey,
                        IsRequired: f.IsRequired,
                        DisplayOrder: f.DisplayOrder,
                        Placeholder: f.Placeholder))
                    .ToList());
        }

        QuizRedirectResponse? redirect = null;
        if (node.Redirect is not null)
        {
            redirect = new QuizRedirectResponse(
                RedirectUrl: node.Redirect.RedirectUrl,
                AutoRedirectAfterSeconds: node.Redirect.AutoRedirectAfterSeconds,
                Tier: node.Redirect.Tier.ToString(),
                Links: node.Redirect.Links
                    .OrderBy(l => l.DisplayOrder)
                    .Select(l => new QuizRedirectLinkResponse(l.Label, l.Url, l.DisplayOrder))
                    .ToList());
        }

        return new CurrentNodeResponse(
            Id: node.Id,
            Type: node.Type.ToString(),
            AttributeKey: node.AttributeKey,
            AnswerType: node.AnswerType?.ToString(),
            ValueKind: node.ValueKind?.ToString(),
            SliderMin: node.SliderMin,
            SliderMax: node.SliderMax,
            Title: node.Title,
            Description: node.Description,
            MediaUrl: node.MediaUrl,
            Options: options,
            Offers: offers,
            LeadCapture: leadCapture,
            Redirect: redirect);
    }

    private async Task TryCreateLeadAsync(
      UserSession session, Guid terminalNodeId,
      Dictionary<string, string> answerContext,
      CancellationToken ct)
    {
        // answerContext already contains all previous DB answers + the current
        // request's LeadCapture fields (injected before SaveChanges).
        var answerMap = answerContext;

        if (!answerMap.TryGetValue("lead_email", out var email)
            || string.IsNullOrWhiteSpace(email))
            return;

        // Avoid duplicate leads if use case is retried
        var existing = await _leads.GetBySessionIdAsync(session.Id, ct);
        if (existing is not null) return;

        var flow = await _flows.GetByIdAsync(session.FlowId, ct);
        if (flow is null) return;

        var terminalNode = await _nodes.GetByIdAsync(terminalNodeId, ct);
        if (terminalNode is null) return;

        // Derive tier from terminal node
        var tier = terminalNode.Type == NodeType.Redirect && terminalNode.Redirect is not null
            ? terminalNode.Redirect.Tier
            : QualificationTier.Hot;  // Offer nodes default to Hot

        var timeToComplete = (int)(DateTime.UtcNow - session.StartedAt).TotalSeconds;

        var lead = Lead.Create(
            sessionId: session.Id,
            flowId: session.FlowId,
            flowOwnerId: flow.OwnerId,
            email: email,
            score: session.Score,
            tier: tier,
            terminalNodeId: terminalNodeId,
            terminalNodeType: terminalNode.Type,
            timeToCompleteSeconds: timeToComplete);

        // Populate identity fields from captured answers
        lead.SetIdentity(
            fullName: answerMap.GetValueOrDefault("lead_name"),
            phone: answerMap.GetValueOrDefault("lead_phone"),
            company: answerMap.GetValueOrDefault("lead_company"),
            jobTitle: answerMap.GetValueOrDefault("lead_title"),
            companySize: answerMap.GetValueOrDefault("lead_company_size"),
            website: answerMap.GetValueOrDefault("lead_website"));

        await _leads.AddAsync(lead, ct);
    }
}

internal record OldEdgeCondition(string AttributeKey, string? Operator, string Value, string? ValueTo);
internal record NewEdgeRule(string AttributeKey, string Operator, string Value, string? ValueTo);
internal record NewEdgeConditionWrapper(string Operator, List<NewEdgeRule> Rules);