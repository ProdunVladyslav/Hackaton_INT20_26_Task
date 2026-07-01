using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Domain.Model.User;
using Infrastructure.Contracts.Quiz.Responses;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Quiz;

/// <summary>
/// Use case: Go back to the previous node in a quiz session.
///
/// Logic:
/// 1. Load session, verify it's InProgress
/// 2. Load all answers ordered by date
/// 3. If no answers, return error (at beginning)
/// 4. Delete the last answer and rewind score delta
/// 5. Move session back to the previous node
/// 6. Return updated session state
/// </summary>
public sealed class GoBackUseCase(
    IUserSessionRepository _sessions,
    IUserAnswerRepository _answers,
    INodeRepository _nodes,
    INodeOfferRepository _nodeOffers,
    IUnitOfWork _uow)
{
    public async Task<FlowResult<SessionStateResponse>> ExecuteAsync(
        Guid sessionId, CancellationToken ct = default)
    {
        var session = await _sessions.GetByIdAsync(sessionId, ct);
        if (session is null)
            return FlowResult<SessionStateResponse>.NotFound("Session not found.");

        if (session.Status != SessionStatus.InProgress)
            return FlowResult<SessionStateResponse>.Fail("Session is not active.", 422);

        var answers = await _answers.GetBySessionOrderedAsync(sessionId, ct);

        if (answers.Count == 0)
            return FlowResult<SessionStateResponse>.Fail("Already at the beginning.", 422);

        var lastAnswer = answers.Last();
        var previousNodeId = lastAnswer.NodeId;

        // Rewind score — reverse the delta that was applied when this answer was submitted
        var previousNode = await _nodes.GetByIdAsync(previousNodeId, ct);
        if (previousNode is
            {
                Type: NodeType.Question,
                AnswerType: AnswerType.SingleChoice or AnswerType.MultipleChoice
            })
        {
            var selectedValues = lastAnswer.Value
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            var delta = previousNode.Options
                .Where(o => selectedValues.Contains(o.Value, StringComparer.OrdinalIgnoreCase))
                .Sum(o => o.ScoreDelta);

            session.AddScore(-delta);
        }

        _answers.Remove(lastAnswer);
        session.MoveToNode(previousNodeId);

        _sessions.Update(session);
        await _uow.SaveChangesAsync(ct);

        var currentNode = session.CurrentNodeId.HasValue
            ? await BuildCurrentNodeAsync(session.CurrentNodeId.Value, ct)
            : null;

        if (currentNode is null)
            return FlowResult<SessionStateResponse>.NotFound("Current node not found.");

        return FlowResult<SessionStateResponse>.Ok(new SessionStateResponse(
            SessionId: session.Id,
            FlowId: session.FlowId,
            Status: session.Status.ToString(),
            StartedAt: session.StartedAt,
            CompletedAt: session.CompletedAt,
            CurrentNode: currentNode
        ));
    }

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
                DisqualificationReason: node.Redirect.DisqualificationReason,
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
}