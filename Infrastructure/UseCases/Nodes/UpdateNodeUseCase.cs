using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Nodes.Requests;
using Infrastructure.Contracts.Nodes.Responses;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Nodes;

/// <summary>
/// Use case: update a node's properties (title, attributeKey, description, media).
/// Only provided fields are updated.
/// </summary>
public sealed class UpdateNodeUseCase
{
    private readonly INodeRepository _nodes;
    private readonly IOptionRepository _options;
    private readonly INodeOfferRepository _nodeOffers;
    private readonly IOfferRepository _offerRepo;
    private readonly IUnitOfWork _uow;

    public UpdateNodeUseCase(
        INodeRepository nodes,
        IOptionRepository options,
        INodeOfferRepository nodeOffers,
        IOfferRepository offerRepo,
        IUnitOfWork uow)
    {
        _nodes = nodes;
        _options = options;
        _nodeOffers = nodeOffers;
        _offerRepo = offerRepo;
        _uow = uow;
    }

    public async Task<FlowResult<NodeResponse>> ExecuteAsync(
        Guid flowId,
        Guid nodeId,
        UpdateNodeRequest request,
        CancellationToken ct = default)
    {
        // Load node
        var node = await _nodes.GetByIdAsync(nodeId, ct);
        if (node == null)
            return FlowResult<NodeResponse>.NotFound("Node not found.");

        // Verify node belongs to the flow
        if (node.FlowId != flowId)
            return FlowResult<NodeResponse>.NotFound("Node not found in this flow.");

        // Apply updates
        try
        {
            if (!string.IsNullOrWhiteSpace(request.Title))
                node.SetTitle(request.Title);

            if (request.AttributeKey != null)
                node.SetAttributeKey(request.AttributeKey);

            if (request.Description != null)
                node.SetDescription(request.Description);

            if (request.MediaUrl != null)
                node.SetMedia(request.MediaUrl);

            // Answer type: clear if ClearAnswerType=true, otherwise update if provided
            if (request.ClearAnswerType)
            {
                node.SetAnswerType(null);
            }
            else if (request.AnswerType != null)
            {
                if (!Enum.TryParse<Domain.Model.Survey.AnswerType>(request.AnswerType, ignoreCase: true, out var answerType))
                    return FlowResult<NodeResponse>.Fail(
                        $"Invalid answer type '{request.AnswerType}'. Must be one of: {string.Join(", ", Enum.GetNames(typeof(Domain.Model.Survey.AnswerType)))}",
                        statusCode: 400);

                // Wipe existing options when switching to Slider
                if (answerType == Domain.Model.Survey.AnswerType.Slider && node.Options.Count > 0)
                {
                    _options.RemoveRange(node.Options);
                }

                node.SetAnswerType(answerType, request.SliderMin, request.SliderMax);
            }

            // Update linked offer if this is an Offer node and offer data was provided
            if (request.Offer is { } offerReq && node.Type == NodeType.Offer)
            {
                var nodeOfferLinks = await _nodeOffers.GetByNodeIdWithOfferAsync(nodeId, ct);
                var primaryLink = nodeOfferLinks.FirstOrDefault(x => x.Link.IsPrimary);
                var offer = primaryLink.Offer ?? nodeOfferLinks.FirstOrDefault().Offer;

                if (offer != null)
                {
                    if (offerReq.CtaText is not null)
                        offer.SetCta(offerReq.CtaText, offerReq.CtaUrl ?? offer.CtaUrl);
                    if (offerReq.CtaUrl is not null && offerReq.CtaText is null)
                        offer.SetCta(offer.CtaText, offerReq.CtaUrl);
                    if (offerReq.Price.HasValue)
                        offer.SetPrice(offerReq.Price.Value);
                    if (offerReq.PhysicalWellnessKitName is not null)
                        offer.SetPhysicalWellnessKitName(offerReq.PhysicalWellnessKitName);
                    if (offerReq.PhysicalWellnessKitItems is not null)
                        offer.SetPhysicalWellnessKitItems(offerReq.PhysicalWellnessKitItems);
                    if (offerReq.Description is not null)
                        offer.SetDescription(offerReq.Description);
                    if (offerReq.Duration is not null)
                        offer.SetDuration(offerReq.Duration);
                    if (offerReq.DigitalContent is not null)
                        offer.SetDigitalContent(offerReq.DigitalContent);
                    if (offerReq.ImageUrl is not null)
                        offer.SetImageUrl(offerReq.ImageUrl);

                    _offerRepo.Update(offer);
                }
            }

            _nodes.Update(node);
            await _uow.SaveChangesAsync();

            return FlowResult<NodeResponse>.Ok(ToResponse(node));
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

    private static NodeResponse ToResponse(Node node) =>
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
            node.SliderMax);
}
