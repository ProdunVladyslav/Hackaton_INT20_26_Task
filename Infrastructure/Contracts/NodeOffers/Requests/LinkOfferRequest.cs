using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Contracts.NodeOffers.Requests;

public sealed record LinkOfferRequest(
    [Required] Guid OfferId,
    bool IsPrimary = false
);
