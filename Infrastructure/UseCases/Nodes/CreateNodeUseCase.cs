using Application.Repositories.Interfaces;
using Domain;
using Domain.Model.Survey;
using Domain.Services;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.Nodes.Requests;
using Infrastructure.Contracts.Nodes.Responses;
using Infrastructure.Contracts.Offers.Responses;

namespace Infrastructure.UseCases.Nodes;

/// <summary>
/// Use case: create a new node in a flow.
/// When type is "Offer" and an inline Offer object is provided, also creates the offer
/// and links it to the node in a single transaction.
/// </summary>
public sealed class CreateNodeUseCase(
    IFlowRepository _flows,
    INodeRepository _nodes,
    IOfferRepository _offers,
    INodeOfferRepository _nodeOffers,
    IUserProfileRepository _userProfiles,
    NodeFactory _factory,
    IUnitOfWork _uow)
{
    public async Task<FlowResult<NodeResponse>> ExecuteAsync(
        Guid flowId,
        Guid applicationUserId,
        CreateNodeRequest request,
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

        var existingNodes = await _nodes.GetByFlowAsync(flowId, ct);

        // ── 2. Parse type ─────────────────────────────────────────────────────
        if (!Enum.TryParse<NodeType>(request.Type, ignoreCase: true, out var nodeType))
            return FlowResult<NodeResponse>.Fail(
                $"Invalid node type '{request.Type}'.", 400);

        // ── 3. Factory creates + validates ────────────────────────────────────
        try
        {
            var fieldDefinitions = request.Fields?
                .Select(f => new LeadCaptureFieldDefinition(
                    Enum.Parse<LeadCaptureFieldType>(f.FieldType, ignoreCase: true),
                    f.IsRequired,
                    f.DisplayOrder,
                    f.Placeholder))
                ?? [];

            var linkDefinitions = request.Links?
                .Select(l => new NodeRedirectLinkDefinition(l.Label, l.Url))
                ?? [];

            Node node = nodeType switch
            {
                NodeType.Question => _factory.CreateQuestion(
                    flowId,
                    existingNodes,
                    request.Title,
                    request.AttributeKey
                        ?? throw new DomainException("Question nodes require AttributeKey."),
                    Enum.Parse<AnswerType>(request.AnswerType
                        ?? throw new DomainException("Question nodes require AnswerType."),
                        ignoreCase: true),
                    Enum.Parse<ValueKind>(request.ValueKind
                        ?? throw new DomainException("Question nodes require ValueKind."),
                        ignoreCase: true),
                    request.PositionX,
                    request.PositionY,
                    request.Description,
                    request.MediaUrl,
                    request.SliderMin,
                    request.SliderMax),

                NodeType.InfoPage => _factory.CreateInfoPage(
                    flowId, request.Title,
                    request.PositionX, request.PositionY,
                    request.Description, request.MediaUrl),

                NodeType.LeadCapture => _factory.CreateLeadCapture(
                    flowId, request.Title,
                    request.PositionX, request.PositionY,
                    request.IsRequired ?? true,
                    fieldDefinitions,
                    request.Description, request.MediaUrl),

                NodeType.Offer => _factory.CreateOffer(
                    flowId, request.Title,
                    request.PositionX, request.PositionY,
                    request.Description, request.MediaUrl),

                NodeType.Redirect => _factory.CreateRedirect(
                    flowId, 
                    request.Title,
                    request.DisqualificationReason
                        ?? "Not a fit",
                    request.PositionX, 
                    request.PositionY,
                    request.RedirectUrl,
                    request.AutoRedirectAfterSeconds,
                    linkDefinitions,
                    request.Description, request.MediaUrl),

                _ => throw new DomainException($"Unknown node type: {nodeType}")
            };

            await _nodes.AddAsync(node, ct);

            // ── 4. Inline offer (Offer nodes only) ───────────────────────────────
            Offer? linkedOffer = null;
            if (request.Offer is { } offerReq && nodeType == NodeType.Offer)
            {
                var offerName = offerReq.Name ?? request.Title;
                var slug = !string.IsNullOrWhiteSpace(offerReq.Slug)
                    ? offerReq.Slug
                    : GenerateSlug(offerName);

                if (await _offers.SlugExistsAsync(slug, null, ct))
                    slug = $"{slug}-{Guid.NewGuid().ToString("N")[..8]}";

                var offer = Offer.Create(slug, offerName, profile.Id);

                if (offerReq.Headline is not null) offer.SetHeadline(offerReq.Headline);
                if (offerReq.Body is not null) offer.SetBody(offerReq.Body);
                if (offerReq.ImageUrl is not null) offer.SetImageUrl(offerReq.ImageUrl);
                if (offerReq.CalendarUrl is not null) offer.SetCalendarUrl(offerReq.CalendarUrl);

                var ctaUrl = offerReq.CtaUrl ?? offerReq.CalendarUrl ?? "";
                offer.SetCta(offerReq.CtaText ?? "Learn More", ctaUrl);

                await _offers.AddAsync(offer, ct);
                await _uow.SaveChangesAsync(ct);

                var nodeOffer = NodeOffer.Create(node.Id, offer.Id, offerReq.IsPrimary);

                if (offerReq.Tier is not null && Enum.TryParse<QualificationTier>(offerReq.Tier, ignoreCase: true, out var tier))
                {
                    nodeOffer.SetTier(tier);
                }
                else
                {
                    nodeOffer.SetTier(QualificationTier.Warm); // Default tier
                }

                if (offerReq.CalendarProvider is not null && Enum.TryParse<CalendarProvider>(offerReq.CalendarProvider, ignoreCase: true, out var calendarProvider))
                    nodeOffer.SetCalendarProvider(calendarProvider);

                await _nodeOffers.AddAsync(nodeOffer, ct);

                linkedOffer = offer;
            }

            await _uow.SaveChangesAsync(ct);

            return FlowResult<NodeResponse>.Ok(ToResponse(node, linkedOffer));
        }
        catch (DomainException ex)
        {
            return FlowResult<NodeResponse>.Fail(ex.Message, 422);
        }
        catch (ArgumentException ex)
        {
            return FlowResult<NodeResponse>.Fail(ex.Message, 400);
        }
    }

    private static string GenerateSlug(string name) =>
        System.Text.RegularExpressions.Regex
            .Replace(name.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "-")
            .Trim('-');

    private static NodeResponse ToResponse(Node node, Offer? linkedOffer = null) => new(
        node.Id, node.FlowId, node.Type.ToString(),
        node.AttributeKey, node.Title, node.Description,
        node.MediaUrl, node.PositionX, node.PositionY,
        node.CreatedAt, node.AnswerType?.ToString(),
        node.SliderMin, node.SliderMax,
        linkedOffer is null ? null : new OfferResponse(
            linkedOffer.Id, linkedOffer.Slug, linkedOffer.Name,
            linkedOffer.Headline, linkedOffer.Body,
            linkedOffer.ImageUrl, linkedOffer.CalendarUrl,
            linkedOffer.CtaText, linkedOffer.CtaUrl));
}
