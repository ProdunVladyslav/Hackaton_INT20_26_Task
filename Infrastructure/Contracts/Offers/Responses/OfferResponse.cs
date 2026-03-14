namespace Infrastructure.Contracts.Offers.Responses;

public sealed record OfferResponse(
    Guid Id,
    string Slug,
    string Name,
    string? Description,
    string? Duration,
    string? DigitalContent,
    string? KitName,
    string? KitContents,
    decimal? Price,
    string? ImageUrl,
    string? CtaText,
    string? CtaUrl
);
