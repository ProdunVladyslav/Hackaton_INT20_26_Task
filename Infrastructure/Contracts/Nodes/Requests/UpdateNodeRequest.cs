namespace Infrastructure.Contracts.Nodes.Requests;

public sealed record UpdateNodeRequest(
    string? Title,
    string? AttributeKey,
    string? Description,
    string? MediaUrl,
    /// <summary>SingleChoice | MultipleChoice | Slider | null to clear. Only valid for Question nodes.</summary>
    string? AnswerType = null,
    decimal? SliderMin = null,
    decimal? SliderMax = null,
    /// <summary>Set true to explicitly clear the AnswerType (needed to distinguish "not sent" from "clear").</summary>
    bool ClearAnswerType = false,
    /// <summary>When updating an Offer node, update the linked offer's fields.</summary>
    UpdateInlineOfferRequest? Offer = null
);

/// <summary>Offer fields to update when editing an Offer node.</summary>
public sealed record UpdateInlineOfferRequest(
    string? CtaText = null,
    string? CtaUrl = null,
    decimal? Price = null,
    string? PhysicalWellnessKitName = null,
    string? PhysicalWellnessKitItems = null,
    string? Description = null,
    string? Duration = null,
    string? DigitalContent = null,
    string? ImageUrl = null
);
