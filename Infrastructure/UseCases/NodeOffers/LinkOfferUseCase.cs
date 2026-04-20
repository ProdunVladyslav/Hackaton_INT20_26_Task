using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.NodeOffers.Requests;
using Infrastructure.Contracts.NodeOffers.Responses;

namespace Infrastructure.UseCases.NodeOffers;

public sealed class LinkOfferUseCase(
    INodeRepository _nodeRepository,
    IOfferRepository _offerRepository,
    INodeOfferRepository _nodeOfferRepository,
    IFlowRepository _flowRepository,
    IUserProfileRepository _userProfiles,
    IUnitOfWork _unitOfWork)
{
    public async Task<FlowResult<NodeOfferResponse>> ExecuteAsync(
        Guid nodeId,
        Guid applicationUserId,
        LinkOfferRequest request,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(
            p => p.ApplicationUserId == applicationUserId, ct);
        if (profile is null)
            return FlowResult<NodeOfferResponse>.NotFound("User profile not found.");

        var node = await _nodeRepository.GetByIdAsync(nodeId, ct);
        if (node is null)
            return FlowResult<NodeOfferResponse>.NotFound("Node not found.");

        if (node.Type != NodeType.Offer)
            return FlowResult<NodeOfferResponse>.Fail(
                "Offers can only be linked to Offer nodes.", 422);

        var flow = await _flowRepository.FirstOrDefaultAsync(
            f => f.Id == node.FlowId && f.OwnerId == profile.Id, ct);
        if (flow is null)
            return FlowResult<NodeOfferResponse>.NotFound("Node not found.");

        var offer = await _offerRepository.GetByIdAsync(request.OfferId, ct);
        if (offer is null)
            return FlowResult<NodeOfferResponse>.NotFound("Offer not found.");

        var alreadyLinked = await _nodeOfferRepository.AnyAsync(
            no => no.NodeId == nodeId && no.OfferId == request.OfferId, ct);
        if (alreadyLinked)
            return FlowResult<NodeOfferResponse>.Fail(
                "Offer is already linked to this node.", 409);

        try
        {
            var nodeOffer = NodeOffer.Create(nodeId, request.OfferId, request.IsPrimary);

            if (request.Tier is not null
                && Enum.TryParse<QualificationTier>(request.Tier, ignoreCase: true, out var tier))
                nodeOffer.SetTier(tier);

            if (request.CalendarProvider is not null
                && Enum.TryParse<CalendarProvider>(request.CalendarProvider, ignoreCase: true, out var provider))
                nodeOffer.SetCalendarProvider(provider);

            if (request.AssignedOwnerId is not null)
                nodeOffer.SetAssignedOwner(request.AssignedOwnerId);

            await _nodeOfferRepository.AddAsync(nodeOffer, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return FlowResult<NodeOfferResponse>.Ok(ToResponse(nodeOffer, offer));
        }
        catch (ArgumentException ex)
        {
            return FlowResult<NodeOfferResponse>.Fail(ex.Message, 400);
        }
    }

    private static NodeOfferResponse ToResponse(NodeOffer no, Offer offer) =>
        new(no.Id, no.NodeId, no.OfferId, no.IsPrimary,
            no.Tier.ToString(), no.CalendarProvider?.ToString(),
            no.AssignedOwnerId, offer.Name, offer.Slug);
}