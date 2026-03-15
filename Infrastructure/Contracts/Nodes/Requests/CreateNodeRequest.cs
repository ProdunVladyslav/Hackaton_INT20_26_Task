using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Contracts.Nodes.Requests;

public sealed record CreateNodeRequest(
    [Required] string Type,
    [Required][MaxLength(500)] string Title,
    string? AttributeKey,
    string? Description,
    string? MediaUrl,
    float PositionX = 0f,
    float PositionY = 0f,
    /// <summary>SingleChoice | MultipleChoice | Slider. Only valid for Question nodes.</summary>
    string? AnswerType = null,
    decimal? SliderMin = null,
    decimal? SliderMax = null,
    /// <summary>
    /// Optional inline offer. When type is "Offer" and this is provided, the offer is created
    /// and linked to the node automatically in a single request.
    /// </summary>
    InlineOfferRequest? Offer = null
);

/// <summary>Offer data to create and link inline when creating an Offer node.</summary>
public sealed record InlineOfferRequest(
    /// <summary>URL-safe slug. Auto-generated from Name (or node Title) if omitted.</summary>
    string? Slug = null,
    [MaxLength(300)] string? Name = null,
    string? Description = null,
    string? Duration = null,
    string? DigitalContent = null,
    string? PhysicalWellnessKitName = null,
    string? PhysicalWellnessKitItems = null,
    decimal? Price = null,
    string? ImageUrl = null,
    string? CtaText = null,
    string? CtaUrl = null,
    bool IsPrimary = true
);
