namespace Infrastructure.Contracts.Analytics.Responses;

public sealed record OfferStatsResponse(List<OfferStatItem> Items);

public sealed record OfferStatItem(
    Guid OfferId,
    string OfferName,
    string OfferSlug,
    int TimesPresented,
    int TimesConverted,
    double ConversionRate
);
