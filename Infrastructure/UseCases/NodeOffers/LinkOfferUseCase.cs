using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.NodeOffers.Requests;
using Infrastructure.Contracts.NodeOffers.Responses;

namespace Infrastructure.UseCases.NodeOffers;

public sealed class LinkOfferUseCase
{
    private readonly INodeRepository _nodeRepository;
    private readonly IOfferRepository _offerRepository;
    private readonly INodeOfferRepository _nodeOfferRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LinkOfferUseCase(
        INodeRepository nodeRepository,
        IOfferRepository offerRepository,
        INodeOfferRepository nodeOfferRepository,
        IUnitOfWork unitOfWork)
    {
        _nodeRepository = nodeRepository;
        _offerRepository = offerRepository;
        _nodeOfferRepository = nodeOfferRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<FlowResult<NodeOfferResponse>> ExecuteAsync(Guid nodeId, LinkOfferRequest request, CancellationToken ct = default)
    {
        // Check node exists
        var node = await _nodeRepository.GetByIdAsync(nodeId, ct);
        if (node is null)
            return FlowResult<NodeOfferResponse>.NotFound("Node not found.");

        // Check offer exists
        var offer = await _offerRepository.GetByIdAsync(request.OfferId, ct);
        if (offer is null)
            return FlowResult<NodeOfferResponse>.NotFound("Offer not found.");

        // Check not already linked
        var alreadyLinked = await _nodeOfferRepository.AnyAsync(
            no => no.NodeId == nodeId && no.OfferId == request.OfferId,
            ct);

        if (alreadyLinked)
            return FlowResult<NodeOfferResponse>.Fail(
                "Offer is already linked to this node.",
                409);

        // Create link
        var nodeOffer = NodeOffer.Create(nodeId, request.OfferId, request.IsPrimary);

        // Add and save
        await _nodeOfferRepository.AddAsync(nodeOffer, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Return response with offer details
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
