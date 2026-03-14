using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.NodeOffers.Responses;

namespace Infrastructure.UseCases.NodeOffers;

public sealed class ListNodeOffersUseCase
{
    private readonly INodeRepository _nodeRepository;
    private readonly INodeOfferRepository _nodeOfferRepository;

    public ListNodeOffersUseCase(INodeRepository nodeRepository, INodeOfferRepository nodeOfferRepository)
    {
        _nodeRepository = nodeRepository;
        _nodeOfferRepository = nodeOfferRepository;
    }

    public async Task<FlowResult<List<NodeOfferResponse>>> ExecuteAsync(Guid nodeId, CancellationToken ct = default)
    {
        // Check node exists
        var node = await _nodeRepository.GetByIdAsync(nodeId, ct);
        if (node is null)
            return FlowResult<List<NodeOfferResponse>>.NotFound("Node not found.");

        // Load node offers with offer details
        var nodeOffers = await _nodeOfferRepository.GetByNodeIdWithOfferAsync(nodeId, ct);

        var responses = nodeOffers.Select(no => new NodeOfferResponse(
            Id: no.Link.Id,
            NodeId: no.Link.NodeId,
            OfferId: no.Link.OfferId,
            IsPrimary: no.Link.IsPrimary,
            OfferName: no.Offer.Name,
            OfferSlug: no.Offer.Slug,
            OfferPrice: no.Offer.Price
        )).ToList();

        return FlowResult<List<NodeOfferResponse>>.Ok(responses);
    }
}
