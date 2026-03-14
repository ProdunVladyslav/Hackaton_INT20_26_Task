namespace Infrastructure.Contracts.Offers.Requests;

public sealed record UpdateOfferRequest(
    string? Slug = null,
    string? Name = null,
    string? Description = null,
    string? Duration = null,
    string? DigitalContent = null,
    string? KitName = null,
    string? KitContents = null,
    decimal? Price = null,
    string? ImageUrl = null,
    string? CtaText = null,
    string? CtaUrl = null
);
