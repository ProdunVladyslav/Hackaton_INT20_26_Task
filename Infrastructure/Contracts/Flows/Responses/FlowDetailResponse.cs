using Domain.Model.Survey;

namespace Infrastructure.Contracts.Flows.Responses;

// ── GetFlowUseCase ────────────────────────────────────────────────────────────

/// <summary>
/// Full flow DAG: nodes (with offers, redirects, lead-capture config) + edges.
/// No analytics — use <see cref="FlowStatsResponse"/> for that.
/// </summary>
public sealed record FlowDetailResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsPublished,
    Guid? EntryNodeId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<NodeDto> Nodes,
    List<EdgeDto> Edges,
    IReadOnlyList<AttributeKeyDto> AttributeKeys  // derived from domain metadata, not stored in DB
);

public sealed record NodeDto(
    Guid Id,
    string Type,
    string? AttributeKey,
    string? ValueKind,
    string Title,
    string? Description,
    string? MediaUrl,
    float PositionX,
    float PositionY,
    DateTime CreatedAt,
    string? AnswerType,
    decimal? SliderMin,
    decimal? SliderMax,
    List<OptionDto> Options,
    List<NodeOfferDto> NodeOffers,
    NodeRedirectDto? Redirect,
    NodeLeadCaptureDto? LeadCapture
);

public sealed record EdgeDto(
    Guid Id,
    Guid SourceNodeId,
    Guid TargetNodeId,
    int Priority,
    string? Conditions
);

public sealed record OptionDto(
    Guid Id,
    string Label,
    string Value,
    int DisplayOrder,
    int? ScoreDelta,
    string? MediaUrl
);

public sealed record NodeOfferDto(
    Guid Id,
    Guid OfferId,
    bool IsPrimary,
    OfferDto Offer
);

public sealed record OfferDto(
    Guid Id,
    string Slug,
    string Name,
    string? Headline,
    string? Body,
    string? ImageUrl,
    string? CalendarUrl,
    string? CalendarProvider,
    string? Tier,
    string? CtaText,
    string? CtaUrl
);

public sealed record NodeRedirectDto(
    Guid Id,
    string? RedirectUrl,
    int? AutoRedirectAfterSeconds,
    string? DisqualificationReason,
    IReadOnlyList<NodeRedirectLinkDto> Links
);

public sealed record NodeRedirectLinkDto(
    Guid Id,
    string Label,
    string Url
);

public sealed record NodeLeadCaptureDto(
    Guid Id,
    bool IsRequired,
    IReadOnlyList<NodeLeadCaptureFieldDto> Fields
);

public sealed record NodeLeadCaptureFieldDto(
    Guid Id,
    string FieldType,
    string AttributeKey,
    bool IsRequired,
    int DisplayOrder,
    string Placeholder
);

public record AttributeKeyDto(
    string Key,
    string ValueKind,
    string[] AllowedOperators
);