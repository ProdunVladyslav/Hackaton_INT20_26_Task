using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Nodes.Requests;
using Infrastructure.Contracts.Nodes.Responses;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.Offers.Responses;

namespace Infrastructure.UseCases.Nodes;

/// <summary>
/// Use case: create a new node in a flow.
/// When type is "Offer" and an inline Offer object is provided, also creates the offer
/// and links it to the node in a single transaction.
/// </summary>
public sealed class CreateNodeUseCase
{
    private readonly IFlowRepository _flows;
    private readonly INodeRepository _nodes;
    private readonly IOfferRepository _offers;
    private readonly INodeOfferRepository _nodeOffers;
    private readonly IUnitOfWork _uow;

    public CreateNodeUseCase(
        IFlowRepository flows,
        INodeRepository nodes,
        IOfferRepository offers,
        INodeOfferRepository nodeOffers,
        IUnitOfWork uow)
    {
        _flows = flows;
        _nodes = nodes;
        _offers = offers;
        _nodeOffers = nodeOffers;
        _uow = uow;
    }

    public async Task<FlowResult<NodeResponse>> ExecuteAsync(
        Guid flowId,
        CreateNodeRequest request,
        CancellationToken ct = default)
    {
        // Validate flow exists
        var flow = await _flows.GetByIdAsync(flowId, ct);
        if (flow == null)
            return FlowResult<NodeResponse>.NotFound("Flow not found.");

        // Parse NodeType enum
        if (!Enum.TryParse<NodeType>(request.Type, ignoreCase: true, out var nodeType))
            return FlowResult<NodeResponse>.Fail(
                $"Invalid node type '{request.Type}'. Must be one of: {string.Join(", ", Enum.GetNames(typeof(NodeType)))}",
                statusCode: 400);

        // Inline offer only makes sense for Offer nodes
        if (request.Offer != null && nodeType != NodeType.Offer)
            return FlowResult<NodeResponse>.Fail(
                "Inline offer can only be provided for nodes of type 'Offer'.",
                statusCode: 400);

        try
        {
            var node = Node.Create(
                flowId,
                nodeType,
                request.Title,
                request.AttributeKey ?? "",
                request.PositionX,
                request.PositionY);

            if (!string.IsNullOrWhiteSpace(request.Description))
                node.SetDescription(request.Description);

            if (!string.IsNullOrWhiteSpace(request.MediaUrl))
                node.SetMedia(request.MediaUrl);

            if (request.AnswerType != null)
            {
                if (!Enum.TryParse<Domain.Model.Survey.AnswerType>(request.AnswerType, ignoreCase: true, out var answerType))
                    return FlowResult<NodeResponse>.Fail(
                        $"Invalid answer type '{request.AnswerType}'. Must be one of: {string.Join(", ", Enum.GetNames(typeof(Domain.Model.Survey.AnswerType)))}",
                        statusCode: 400);

                node.SetAnswerType(answerType, request.SliderMin, request.SliderMax);
            }

            await _nodes.AddAsync(node, ct);

            // ── Inline offer creation ────────────────────────────────────────
            Offer? linkedOffer = null;
            if (request.Offer is { } offerReq)
            {
                var offerName = offerReq.Name ?? request.Title;
                var slug = !string.IsNullOrWhiteSpace(offerReq.Slug)
                    ? offerReq.Slug
                    : GenerateSlug(offerName);

                // Ensure slug uniqueness
                if (await _offers.SlugExistsAsync(slug, null, ct))
                    slug = $"{slug}-{Guid.NewGuid().ToString("N")[..8]}";

                var offer = Offer.Create(slug, offerName);

                if (offerReq.Description is not null) offer.SetDescription(offerReq.Description);
                if (offerReq.Duration is not null) offer.SetDuration(offerReq.Duration);
                if (offerReq.DigitalContent is not null) offer.SetDigitalContent(offerReq.DigitalContent);
                if (offerReq.PhysicalWellnessKitName is not null) offer.SetPhysicalWellnessKitName(offerReq.PhysicalWellnessKitName);
                if (offerReq.PhysicalWellnessKitItems is not null) offer.SetPhysicalWellnessKitItems(offerReq.PhysicalWellnessKitItems);
                if (offerReq.Price.HasValue) offer.SetPrice(offerReq.Price.Value);
                if (offerReq.ImageUrl is not null) offer.SetImageUrl(offerReq.ImageUrl);
                offer.SetCta(offerReq.CtaText ?? "", offerReq.CtaUrl ?? "");

                await _offers.AddAsync(offer, ct);
                await _uow.SaveChangesAsync(ct);

                var nodeOffer = NodeOffer.Create(node.Id, offer.Id, offerReq.IsPrimary);
                await _nodeOffers.AddAsync(nodeOffer, ct);

                linkedOffer = offer;
            }

            await _uow.SaveChangesAsync(ct);

            return FlowResult<NodeResponse>.Ok(ToResponse(node, linkedOffer));
        }
        catch (ArgumentException ex)
        {
            return FlowResult<NodeResponse>.Fail(ex.Message, statusCode: 400);
        }
        catch (InvalidOperationException ex)
        {
            return FlowResult<NodeResponse>.Fail(ex.Message, statusCode: 422);
        }
    }

    private static string GenerateSlug(string name) =>
        System.Text.RegularExpressions.Regex
            .Replace(name.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "-")
            .Trim('-');

    private static NodeResponse ToResponse(Node node, Offer? linkedOffer = null) =>
        new(
            node.Id,
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
            node.SliderMax,
            linkedOffer is null ? null : new OfferResponse(
                linkedOffer.Id,
                linkedOffer.Slug,
                linkedOffer.Name,
                linkedOffer.Description,
                linkedOffer.Duration,
                linkedOffer.DigitalContent,
                linkedOffer.PhysicalWellnessKitName,
                linkedOffer.PhysicalWellnessKitItems,
                linkedOffer.Price,
                linkedOffer.ImageUrl,
                linkedOffer.CtaText,
                linkedOffer.CtaUrl));
}
