using Domain.Model.Survey;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Contracts.Nodes.Requests;

public sealed record CreateNodeRequest(
    [Required] string Type,
    [Required][MaxLength(500)] string Title,
    float PositionX = 0f,
    float PositionY = 0f,
    string? Description = null,
    string? MediaUrl = null,

    // ── Question ──────────────────────────────────────────────────────────────
    string? AttributeKey = null,
    /// <summary>SingleChoice | MultipleChoice | Slider | Text</summary>
    string? AnswerType = null,
    string? ValueKind = null,
    decimal? SliderMin = null,
    decimal? SliderMax = null,

    // ── Offer ─────────────────────────────────────────────────────────────────
    /// <summary>
    /// Optional inline offer. When Type is "Offer" and this is provided,
    /// the offer is created and linked in a single request.
    /// </summary>
    InlineOfferRequest? Offer = null,

    // ── LeadCapture ───────────────────────────────────────────────────────────
    /// <summary>
    /// If true, user cannot proceed without submitting the form.
    /// Defaults to true.
    /// </summary>
    bool? IsRequired = null,
    /// <summary>
    /// Fields to show on the LeadCapture form.
    /// Email field must always be included.
    /// </summary>
    List<LeadCaptureFieldRequest>? Fields = null,

    // ── Redirect ──────────────────────────────────────────────────────────────
    /// <summary>Hot | Warm | Cold | Disqualified</summary>
    string? DisqualificationReason = null,
    string? RedirectUrl = null,
    int? AutoRedirectAfterSeconds = null,
    /// <summary>Up to 3 resource links shown on the redirect screen.</summary>
    List<NodeRedirectLinkRequest>? Links = null
);

// ── LeadCapture field ─────────────────────────────────────────────────────────

/// <summary>
/// One field to include on a LeadCapture node form.
/// FieldType maps to: FullName | Email | Phone | CompanyName | JobTitle | CompanySize | Website
/// </summary>
public sealed record LeadCaptureFieldRequest(
    [Required] string FieldType,
    bool IsRequired = false,
    int DisplayOrder = 0,
    string? Placeholder = null
);

// ── Redirect resource link ────────────────────────────────────────────────────

public sealed record NodeRedirectLinkRequest(
    [Required][MaxLength(200)] string Label,
    [Required][MaxLength(1000)] string Url
);

// ── Inline offer ──────────────────────────────────────────────────────────────

/// <summary>Offer data to create and link inline when creating an Offer node.</summary>
public sealed record InlineOfferRequest(
    string? Slug = null,
    [MaxLength(300)] string? Name = null,
    string? Headline = null,
    string? Body = null,
    string? ImageUrl = null,
    string? CalendarUrl = null,
    string? Tier = null,
    string? CalendarProvider = null,
    [MaxLength(300)] string? CtaText = null,
    [MaxLength(1000)] string? CtaUrl = null,
    bool IsPrimary = true
);