namespace Infrastructure.Contracts.NodeOffers.Responses;

public sealed record NodeOfferResponse(
    Guid Id,
    Guid NodeId,
    Guid OfferId,
    bool IsPrimary,
    string? Tier,
    string? CalendarProvider,
    Guid? AssignedOwnerId,
    string OfferName,
    string OfferSlug
);