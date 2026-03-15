namespace Infrastructure.Contracts.Offers.Requests;

public sealed record UpdateOfferRequest(
    string? Slug = null,
    string? Name = null,
    string? Description = null,
    string? Duration = null,
    string? DigitalContent = null,
    string? PhysicalWellnessKitName = null,
    string? PhysicalWellnessKitItems = null,
    decimal? Price = null,
    string? ImageUrl = null,
    string? CtaText = null,
    string? CtaUrl = null
);
