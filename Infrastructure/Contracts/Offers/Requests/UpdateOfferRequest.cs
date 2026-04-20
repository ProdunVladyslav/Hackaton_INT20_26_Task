namespace Infrastructure.Contracts.Offers.Requests;

public sealed record UpdateOfferRequest(
    string? Slug = null,
    string? Name = null,
    string? Headline = null,
    string? Body = null,
    string? ImageUrl = null,
    string? CalendarUrl = null,
    string? CtaText = null,
    string? CtaUrl = null
);