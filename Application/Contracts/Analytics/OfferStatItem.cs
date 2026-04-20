namespace Application.Contracts.Analytics
{
    public sealed record OfferStatItem(
        Guid OfferId,
        string OfferName,
        string OfferSlug,
        Guid FlowId,
        string FlowName,
        int TimesPresented,
        int TimesConverted,
        double ConversionRate
    );
}
