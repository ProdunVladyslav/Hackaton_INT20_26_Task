using Application;
using Application.Repositories.Interfaces;
using Domain.Model.User;
using Infrastructure.Contracts.Quiz.Responses;
using Infrastructure.Contracts.Flows.Responses;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.UseCases.Quiz;

/// <summary>
/// Use case: Get the current state of a quiz session.
///
/// If session is completed, CurrentNode is null.
/// Otherwise, returns the current node with options and offers.
/// </summary>
public sealed class GetSessionUseCase(
    IUserSessionRepository _sessions,
    INodeRepository _nodes,
    INodeOfferRepository _nodeOffers)
{
    public async Task<FlowResult<SessionStateResponse>> ExecuteAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _sessions.GetByIdAsync(sessionId, ct);
        if (session is null)
            return FlowResult<SessionStateResponse>.NotFound("Session not found.");

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

    private async Task<CurrentNodeResponse?> BuildCurrentNodeAsync(Guid nodeId, CancellationToken ct)
    {
        var node = await _nodes.GetWithOptionsAsync(nodeId, ct);
        if (node is null) return null;

        var nodeOfferData = await _nodeOffers.GetByNodeIdWithOfferForQuizAsync(nodeId, ct);

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

        var options = node.Options
            .OrderBy(o => o.DisplayOrder)
            .Select(o => new QuizOptionResponse(
                o.Id, o.Label, o.Value, o.DisplayOrder, o.MediaUrl, o.ScoreDelta))
            .ToList();

        // LeadCapture config — only present on LeadCapture nodes
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

        // Redirect config — only present on Redirect nodes
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
