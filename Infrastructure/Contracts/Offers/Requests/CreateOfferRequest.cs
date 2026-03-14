using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Contracts.Offers.Requests;

public sealed record CreateOfferRequest(
    [Required][MaxLength(200)] string Slug,
    [Required][MaxLength(300)] string Name,
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
