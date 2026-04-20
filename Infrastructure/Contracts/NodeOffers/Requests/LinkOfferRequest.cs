using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Contracts.NodeOffers.Requests;

public sealed record LinkOfferRequest(
    Guid OfferId,
    bool IsPrimary = true,
    string? Tier = null,              // Hot | Warm | Cold | Disqualified
    string? CalendarProvider = null,  // Calendly | CalCom | HubSpot | Custom
    Guid? AssignedOwnerId = null
);
