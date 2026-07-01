using Application.Repositories.Interfaces;
using Domain;
using Domain.Model.Survey;
using Domain.Services;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.Nodes.Requests;
using Infrastructure.Contracts.Nodes.Responses;

namespace Infrastructure.UseCases.Nodes;

/// <summary>
/// Use case: update a node's properties.
/// Only provided fields are updated (patch semantics).
///
/// Immutable after creation: AttributeKey, ValueKind.
/// Mutable within ValueKind family: AnswerType.
/// </summary>
public sealed class UpdateNodeUseCase(
    INodeRepository _nodes,
    IOptionRepository _options,
    INodeOfferRepository _nodeOffers,
    IOfferRepository _offerRepo,
    IFlowRepository _flows,
    IUserProfileRepository _userProfiles,
    IUnitOfWork _uow)
{
    public async Task<FlowResult<NodeResponse>> ExecuteAsync(
        Guid flowId,
        Guid nodeId,
        Guid applicationUserId,
        UpdateNodeRequest request,
        CancellationToken ct = default)
    {
        // ── 1. Auth + ownership ───────────────────────────────────────────────
        var profile = await _userProfiles.FirstOrDefaultAsync(
            p => p.ApplicationUserId == applicationUserId, ct);
        if (profile is null)
            return FlowResult<NodeResponse>.NotFound("User profile not found.");

        var flow = await _flows.FirstOrDefaultAsync(
            f => f.Id == flowId && f.OwnerId == profile.Id, ct);
        if (flow is null)
            return FlowResult<NodeResponse>.NotFound("Flow not found.");

        var node = await _nodes.GetByIdAsync(nodeId, ct);
        if (node is null)
            return FlowResult<NodeResponse>.NotFound("Node not found.");

        if (node.FlowId != flowId)
            return FlowResult<NodeResponse>.NotFound("Node not found in this flow.");

        try
        {
            // ── 2. Common fields (all node types) ─────────────────────────────
            if (!string.IsNullOrWhiteSpace(request.Title))
                node.SetTitle(request.Title);

            if (request.Description is not null)
                node.SetDescription(request.Description);

            if (request.MediaUrl is not null)
                node.SetMedia(request.MediaUrl);

            // ── 3. Question — AnswerType only, within ValueKind family ─────────
            if (node.Type == NodeType.Question)
            {
                if (request.ClearAnswerType)
                {
                    node.SetAnswerType(null);
                }
                else if (request.AnswerType is not null)
                {
                    if (!Enum.TryParse<AnswerType>(request.AnswerType, ignoreCase: true, out var answerType))
                        return FlowResult<NodeResponse>.Fail(
                            $"Invalid answer type '{request.AnswerType}'. " +
                            $"Must be one of: {string.Join(", ", Enum.GetNames(typeof(AnswerType)))}",
                            400);

                    // AnswerType can only change within its ValueKind family.
                    // Slider requires Numeric — cannot switch to it from a Text-kind node.
                    // SingleChoice / MultipleChoice / Text require Text — cannot switch from Numeric.
                    var requiredValueKind = answerType == AnswerType.Slider
                        ? ValueKind.Numeric
                        : ValueKind.Text;

                    if (node.ValueKind.HasValue && node.ValueKind != requiredValueKind)
                        return FlowResult<NodeResponse>.Fail(
                            $"Cannot change AnswerType to '{answerType}' — it requires ValueKind " +
                            $"'{requiredValueKind}' but this node has ValueKind '{node.ValueKind}'. " +
                            "ValueKind is immutable after creation.",
                            422);

                    if (node.AnswerType == AnswerType.Text && answerType != AnswerType.Text)
                        return FlowResult<NodeResponse>.Fail(
                            $"Changing AnswerType from 'Text' to '{answerType}' is not allowed. Create a new node instead.",
                            422);

                    if ((node.AnswerType == AnswerType.MultipleChoice || node.AnswerType == AnswerType.SingleChoice)
                        && answerType != AnswerType.SingleChoice && answerType != AnswerType.MultipleChoice)
                        return FlowResult<NodeResponse>.Fail(
                            $"Changing AnswerType from '{node.AnswerType}' to '{answerType}' is not allowed. Create a new node instead.",
                            422);

                    if (answerType == AnswerType.Slider && node.Options.Count > 0)
                        _options.RemoveRange(node.Options);

                    // ValueKind not accepted from request — always derived from AnswerType.
                    node.SetAnswerType(answerType, request.SliderMin, request.SliderMax);
                }
            }

            // ── 4. Offer node — update linked offer ───────────────────────────
            if (request.Offer is { } offerReq && node.Type == NodeType.Offer)
            {
                var nodeOfferLinks = await _nodeOffers.GetByNodeIdWithOfferAsync(nodeId, ct);

                var resolvedEntry = nodeOfferLinks.FirstOrDefault(x => x.Link.IsPrimary);
                if (resolvedEntry.Link is null)
                    resolvedEntry = nodeOfferLinks.FirstOrDefault();

                var offer = resolvedEntry.Link is not null ? resolvedEntry.Offer : null;
                var nodeOffer = resolvedEntry.Link is not null ? resolvedEntry.Link : null;

                if (offer is not null && nodeOffer is not null)
                {
                    if (offerReq.Headline is not null)
                        offer.SetHeadline(offerReq.Headline);

                    if (offerReq.Body is not null)
                        offer.SetBody(offerReq.Body);

                    if (offerReq.ImageUrl is not null)
                        offer.SetImageUrl(offerReq.ImageUrl);

                    if (offerReq.CalendarUrl is not null)
                        offer.SetCalendarUrl(offerReq.CalendarUrl);

                    if (offerReq.Tier is not null && Enum.TryParse<QualificationTier>(offerReq.Tier, ignoreCase: true, out var tier))
                        nodeOffer.SetTier(tier);

                    if (offerReq.CalendarProvider is not null && Enum.TryParse<CalendarProvider>(offerReq.CalendarProvider, ignoreCase: true, out var calendarProvider))
                        nodeOffer.SetCalendarProvider(calendarProvider);

                    // Both CTA fields updated together to preserve entity invariant
                    if (offerReq.CtaText is not null || offerReq.CtaUrl is not null)
                        offer.SetCta(
                            offerReq.CtaText ?? offer.CtaText,
                            offerReq.CtaUrl ?? offer.CtaUrl);

                    _offerRepo.Update(offer);
                    _nodeOffers.Update(nodeOffer);   // track the NodeOffer mutation too
                }
            }

            // ── 5. Redirect node — update config ──────────────────────────────
            if (node.Type == NodeType.Redirect && node.Redirect is not null)
            {
                if (request.RedirectUrl is not null)
                    node.Redirect.SetRedirectUrl(request.RedirectUrl);

                if (request.AutoRedirectAfterSeconds.HasValue)
                {
                    if (request.AutoRedirectAfterSeconds == -1)
                    {
                        node.Redirect.SetAutoRedirect(null);
                    }
                    else
                    {
                        node.Redirect.SetAutoRedirect(request.AutoRedirectAfterSeconds);
                    }
                }

                if (request.DisqualificationReason is not null)
                    node.Redirect.SetDisqualificationReason(request.DisqualificationReason);
            }

            // ── 6. LeadCapture node — IsRequired only ─────────────────────────
            // Fields are immutable after creation.
            // Delete and recreate the node if the field list needs to change.
            if (node.Type == NodeType.LeadCapture && node.LeadCapture is not null)
            {
                if (request.IsRequired.HasValue)
                    node.LeadCapture.SetRequired(request.IsRequired.Value);
            }

            _nodes.Update(node);
            await _uow.SaveChangesAsync(ct);

            return FlowResult<NodeResponse>.Ok(ToResponse(node));
        }
        catch (DomainException ex)
        {
            return FlowResult<NodeResponse>.Fail(ex.Message, 422);
        }
        catch (ArgumentException ex)
        {
            return FlowResult<NodeResponse>.Fail(ex.Message, 400);
        }
        catch (InvalidOperationException ex)
        {
            return FlowResult<NodeResponse>.Fail(ex.Message, 422);
        }
    }

    private static NodeResponse ToResponse(Node node) =>
        new(node.Id,
            node.FlowId,
            node.Type.ToString(),
            node.AttributeKey,
            node.Title,
            node.Description,
            node.MediaUrl,
            node.PositionX,
            node.PositionY,
            node.CreatedAt,
            node.AnswerType?.ToString(),
            node.SliderMin,
            node.SliderMax);
}