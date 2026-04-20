namespace Infrastructure.Contracts.Offers.Responses;

public sealed record OfferResponse(
    Guid Id,
    string Slug,
    string Name,
    string? Headline,
    string? Body,
    string? ImageUrl,
    string? CalendarUrl,
    string? CtaText,
    string? CtaUrl
);