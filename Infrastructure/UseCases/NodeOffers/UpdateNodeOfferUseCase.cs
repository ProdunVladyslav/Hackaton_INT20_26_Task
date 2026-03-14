using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.NodeOffers.Requests;
using Infrastructure.Contracts.NodeOffers.Responses;

namespace Infrastructure.UseCases.NodeOffers;

public sealed class UpdateNodeOfferUseCase
{
    private readonly INodeOfferRepository _nodeOfferRepository;
    private readonly IOfferRepository _offerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateNodeOfferUseCase(
        INodeOfferRepository nodeOfferRepository,
        IOfferRepository offerRepository,
        IUnitOfWork unitOfWork)
    {
        _nodeOfferRepository = nodeOfferRepository;
        _offerRepository = offerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<FlowResult<NodeOfferResponse>> ExecuteAsync(
        Guid nodeId,
        Guid nodeOfferId,
        UpdateNodeOfferRequest request,
        CancellationToken ct = default)
    {
        // Load the node offer
        var nodeOffer = await _nodeOfferRepository.GetByIdAsync(nodeOfferId, ct);
        if (nodeOffer is null)
            return FlowResult<NodeOfferResponse>.NotFound("NodeOffer not found.");

        // Verify it belongs to the specified node
        if (nodeOffer.NodeId != nodeId)
            return FlowResult<NodeOfferResponse>.NotFound("NodeOffer not found for this node.");

        // Update primary flag
        nodeOffer.SetPrimary(request.IsPrimary);

        // Load offer for response details
        var offer = await _offerRepository.GetByIdAsync(nodeOffer.OfferId, ct);
        if (offer is null)
            return FlowResult<NodeOfferResponse>.Fail("Linked offer not found.", 500);

        // Update and save
        _nodeOfferRepository.Update(nodeOffer);
        await _unitOfWork.SaveChangesAsync(ct);

        // Return response
        var response = new NodeOfferResponse(
            Id: nodeOffer.Id,
            NodeId: nodeOffer.NodeId,
            OfferId: nodeOffer.OfferId,
            IsPrimary: nodeOffer.IsPrimary,
            OfferName: offer.Name,
            OfferSlug: offer.Slug,
            OfferPrice: offer.Price
        );

        return FlowResult<NodeOfferResponse>.Ok(response);
    }
}
