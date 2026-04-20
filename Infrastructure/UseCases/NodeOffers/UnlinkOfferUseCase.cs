using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.NodeOffers;

public sealed class UnlinkOfferUseCase(
    INodeOfferRepository _nodeOfferRepository,
    INodeRepository _nodeRepository,
    IFlowRepository _flowRepository,
    IUserProfileRepository _userProfiles,
    IUnitOfWork _unitOfWork)
{
    public async Task<FlowResult<bool>> ExecuteAsync(
        Guid nodeId,
        Guid nodeOfferId,
        Guid applicationUserId,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(p => p.ApplicationUserId == applicationUserId, ct);
        if (profile == null)
            return FlowResult<bool>.NotFound("User profile not found.");

        var node = await _nodeRepository.GetByIdAsync(nodeId, ct);
        if (node is null)
            return FlowResult<bool>.NotFound("Node not found.");

        var flow = await _flowRepository.FirstOrDefaultAsync(f => f.Id == node.FlowId && f.OwnerId == profile.Id, ct);
        if (flow is null)
            return FlowResult<bool>.NotFound("Node not found.");

        var nodeOffer = await _nodeOfferRepository.GetByIdAsync(nodeOfferId, ct);
        if (nodeOffer is null)
            return FlowResult<bool>.NotFound("NodeOffer not found.");

        if (nodeOffer.NodeId != nodeId)
            return FlowResult<bool>.NotFound("NodeOffer not found for this node.");

        _nodeOfferRepository.Remove(nodeOffer);
        await _unitOfWork.SaveChangesAsync(ct);

        return FlowResult<bool>.Ok(true);
    }
}
