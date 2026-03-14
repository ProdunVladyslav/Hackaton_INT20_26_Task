using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.NodeOffers;

public sealed class UnlinkOfferUseCase
{
    private readonly INodeOfferRepository _nodeOfferRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UnlinkOfferUseCase(INodeOfferRepository nodeOfferRepository, IUnitOfWork unitOfWork)
    {
        _nodeOfferRepository = nodeOfferRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<FlowResult<bool>> ExecuteAsync(
        Guid nodeId,
        Guid nodeOfferId,
        CancellationToken ct = default)
    {
        // Load the node offer
        var nodeOffer = await _nodeOfferRepository.GetByIdAsync(nodeOfferId, ct);
        if (nodeOffer is null)
            return FlowResult<bool>.NotFound("NodeOffer not found.");

        // Verify it belongs to the specified node
        if (nodeOffer.NodeId != nodeId)
            return FlowResult<bool>.NotFound("NodeOffer not found for this node.");

        // Remove and save
        _nodeOfferRepository.Remove(nodeOffer);
        await _unitOfWork.SaveChangesAsync(ct);

        return FlowResult<bool>.Ok(true);
    }
}
