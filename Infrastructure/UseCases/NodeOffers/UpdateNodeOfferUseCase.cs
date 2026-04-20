using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.NodeOffers.Requests;
using Infrastructure.Contracts.NodeOffers.Responses;

namespace Infrastructure.UseCases.NodeOffers;

public sealed class UpdateNodeOfferUseCase(
    INodeOfferRepository _nodeOfferRepository,
    IOfferRepository _offerRepository,
    INodeRepository _nodeRepository,
    IFlowRepository _flowRepository,
    IUserProfileRepository _userProfiles,
    IUnitOfWork _unitOfWork)
{
    public async Task<FlowResult<NodeOfferResponse>> ExecuteAsync(
        Guid nodeId,
        Guid nodeOfferId,
        Guid applicationUserId,
        UpdateNodeOfferRequest request,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(
            p => p.ApplicationUserId == applicationUserId, ct);
        if (profile is null)
            return FlowResult<NodeOfferResponse>.NotFound("User profile not found.");

        var node = await _nodeRepository.GetByIdAsync(nodeId, ct);
        if (node is null)
            return FlowResult<NodeOfferResponse>.NotFound("Node not found.");

        var flow = await _flowRepository.FirstOrDefaultAsync(
            f => f.Id == node.FlowId && f.OwnerId == profile.Id, ct);
        if (flow is null)
            return FlowResult<NodeOfferResponse>.NotFound("Node not found.");

        var nodeOffer = await _nodeOfferRepository.GetByIdAsync(nodeOfferId, ct);
        if (nodeOffer is null || nodeOffer.NodeId != nodeId)
            return FlowResult<NodeOfferResponse>.NotFound("NodeOffer not found.");

        var offer = await _offerRepository.GetByIdAsync(nodeOffer.OfferId, ct);
        if (offer is null)
            return FlowResult<NodeOfferResponse>.Fail("Linked offer not found.", 500);

        try
        {
            if (request.IsPrimary.HasValue)
                nodeOffer.SetPrimary(request.IsPrimary.Value);

            if (request.Tier is not null
                && Enum.TryParse<QualificationTier>(request.Tier, ignoreCase: true, out var tier))
                nodeOffer.SetTier(tier);

            if (request.CalendarProvider is not null
                && Enum.TryParse<CalendarProvider>(request.CalendarProvider, ignoreCase: true, out var provider))
                nodeOffer.SetCalendarProvider(provider);

            // Explicit null = clear the assignment, so check key presence not value
            if (request.AssignedOwnerId != default)
                nodeOffer.SetAssignedOwner(request.AssignedOwnerId);

            _nodeOfferRepository.Update(nodeOffer);
            await _unitOfWork.SaveChangesAsync(ct);

            return FlowResult<NodeOfferResponse>.Ok(new NodeOfferResponse(
                Id: nodeOffer.Id,
                NodeId: nodeOffer.NodeId,
                OfferId: nodeOffer.OfferId,
                IsPrimary: nodeOffer.IsPrimary,
                Tier: nodeOffer.Tier.ToString(),
                CalendarProvider: nodeOffer.CalendarProvider?.ToString(),
                AssignedOwnerId: nodeOffer.AssignedOwnerId,
                OfferName: offer.Name,
                OfferSlug: offer.Slug
            ));
        }
        catch (ArgumentException ex)
        {
            return FlowResult<NodeOfferResponse>.Fail(ex.Message, 400);
        }
    }
}