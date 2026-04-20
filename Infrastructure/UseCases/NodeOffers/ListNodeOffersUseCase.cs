using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.NodeOffers.Responses;

namespace Infrastructure.UseCases.NodeOffers;

public sealed class ListNodeOffersUseCase(
    INodeRepository _nodeRepository,
    INodeOfferRepository _nodeOfferRepository,
    IFlowRepository _flowRepository,
    IUserProfileRepository _userProfiles)
{
    public async Task<FlowResult<List<NodeOfferResponse>>> ExecuteAsync(
        Guid nodeId,
        Guid applicationUserId,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(
            p => p.ApplicationUserId == applicationUserId, ct);
        if (profile is null)
            return FlowResult<List<NodeOfferResponse>>.NotFound("User profile not found.");

        var node = await _nodeRepository.GetByIdAsync(nodeId, ct);
        if (node is null)
            return FlowResult<List<NodeOfferResponse>>.NotFound("Node not found.");

        var flow = await _flowRepository.FirstOrDefaultAsync(
            f => f.Id == node.FlowId && f.OwnerId == profile.Id, ct);
        if (flow is null)
            return FlowResult<List<NodeOfferResponse>>.NotFound("Node not found.");

        var nodeOffers = await _nodeOfferRepository.GetByNodeIdWithOfferAsync(nodeId, ct);

        return FlowResult<List<NodeOfferResponse>>.Ok(
            nodeOffers.Select(no => new NodeOfferResponse(
                Id: no.Link.Id,
                NodeId: no.Link.NodeId,
                OfferId: no.Link.OfferId,
                IsPrimary: no.Link.IsPrimary,
                Tier: no.Link.Tier.ToString(),
                CalendarProvider: no.Link.CalendarProvider?.ToString(),
                AssignedOwnerId: no.Link.AssignedOwnerId,
                OfferName: no.Offer.Name,
                OfferSlug: no.Offer.Slug
            )).ToList());
    }
}