using Application;
using Application.Repositories.Interfaces;
using Domain.Model.User;
using Domain.Services;
using Infrastructure.Contracts.Quiz.Requests;
using Infrastructure.Contracts.Quiz.Responses;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Quiz;

/// <summary>
/// Use case: Start a new quiz session for a given published flow.
///
/// Validation:
/// - Flow must exist and be published
/// - Flow must have an entry node
///
/// Output: SessionStateResponse with the entry node and its options/offers
/// </summary>
public sealed class StartSessionUseCase(
    IFlowRepository _flows,
    IUserSessionRepository _sessions,
    INodeRepository _nodes,
    INodeOfferRepository _nodeOffers,
    ILeadChannelRepository _leadChannels,
    IUnitOfWork _uow,
    IDateTimeProvider _time)
{
    public async Task<FlowResult<SessionStateResponse>> ExecuteAsync(
        StartSessionRequest request,
        CancellationToken ct = default)
    {
        var flow = await _flows.GetByIdAsync(request.FlowId, ct);
        if (flow is null)
            return FlowResult<SessionStateResponse>.NotFound("Flow not found.");

        if (!flow.IsPublished)
            return FlowResult<SessionStateResponse>.Fail("Flow is not published.", 422);

        if (flow.EntryNodeId is null)
            return FlowResult<SessionStateResponse>.Fail("Flow has no entry node.", 422);

        var entryNode = await _nodes.GetByIdAsync(flow.EntryNodeId.Value, ct);
        if (entryNode is null)
            return FlowResult<SessionStateResponse>.Fail("Entry node no longer exists.", 422);

        var session = UserSession.Create(request.FlowId, flow.EntryNodeId.Value, _time);
        session.SetUtm(request.UtmSource, request.UtmCampaign);

        // Only trust a channel id that actually belongs to this flow — a
        // stale/mismatched id is silently dropped rather than failing the
        // survey over an analytics-only field, same posture as UTM.
        if (request.LeadChannelId is not null)
        {
            var channel = await _leadChannels.GetByIdAsync(request.LeadChannelId.Value, ct);
            if (channel is not null && channel.FlowId == request.FlowId && !channel.IsArchived)
                session.SetLeadChannel(channel.Id);
        }

        await _sessions.AddAsync(session, ct);
        await _uow.SaveChangesAsync(ct);

        var currentNode = await BuildCurrentNodeAsync(flow.EntryNodeId.Value, ct);
        if (currentNode is null)
            return FlowResult<SessionStateResponse>.NotFound("Entry node not found.");

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
                    .   OrderBy(l => l.DisplayOrder)
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
