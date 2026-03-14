namespace Infrastructure.Contracts.NodeOffers.Responses;

public sealed record NodeOfferResponse(
    Guid Id,
    Guid NodeId,
    Guid OfferId,
    bool IsPrimary,
    string OfferName,
    string OfferSlug,
    decimal? OfferPrice
);
