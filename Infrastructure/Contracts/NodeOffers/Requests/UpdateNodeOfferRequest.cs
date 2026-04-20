namespace Infrastructure.Contracts.NodeOffers.Requests;

public sealed record UpdateNodeOfferRequest(
    bool? IsPrimary = null,
    string? Tier = null,
    string? CalendarProvider = null,
    Guid? AssignedOwnerId = null
);