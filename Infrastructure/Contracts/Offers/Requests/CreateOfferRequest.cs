using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Contracts.Offers.Requests;

public sealed record CreateOfferRequest(
    [Required][MaxLength(200)] string Slug,
    [Required][MaxLength(300)] string Name,
    string? Headline = null,
    string? Body = null,
    string? ImageUrl = null,
    string? CalendarUrl = null,
    string? CtaText = null,
    string? CtaUrl = null
);