namespace Infrastructure.Contracts.Flows.Responses;

/// <summary>
/// Full flow with all nodes, edges, options, node-offer links, and admin analytics.
/// Used by the admin visual editor to render the entire DAG on canvas.
/// <see cref="Stats"/> is null when fetched via a public route.
/// </summary>
public sealed record FlowDetailResponse(
    Guid            Id,
    string          Name,
    string?         Description,
    bool            IsPublished,
    Guid?           EntryNodeId,
    DateTime        CreatedAt,
    DateTime        UpdatedAt,
    List<NodeDto>   Nodes,
    List<EdgeDto>   Edges,
    FlowAdminStats? Stats,
    IReadOnlyList<PathDistributionEntryDto> PathDistribution, // Enriched from user session data, not stored in DB
    IReadOnlyList<AttributeKeyDto> AttributeKeys // Enriched from domain metadata, not stored in DB
);

public sealed record NodeDto(
    Guid              Id,
    string            Type,
    string?           AttributeKey,
    string?           ValueKind,
    string            Title,
    string?           Description,
    string?           MediaUrl,
    float             PositionX,
    float             PositionY,
    DateTime          CreatedAt,
    string?           AnswerType,
    decimal?          SliderMin,
    decimal?          SliderMax,
    List<OptionDto>   Options,
    List<NodeOfferDto> NodeOffers,
    NodeStatsDto?     Stats
);

public record AttributeKeyDto(
    string Key,
    string ValueKind,
    string[] AllowedOperators
);

public sealed record OfferDto(
    Guid Id,
    string Slug,
    string Name,
    string Description,
    string Duration,
    string DigitalContent,
    string PhysicalWellnessKitName,
    string PhysicalWellnessKitItems,
    decimal? Price,
    string ImageUrl,
    string CtaText,
    string CtaUrl
);

/// <summary>
/// Per-node analytics embedded in <see cref="NodeDto"/> for the admin editor.
/// Lets the UI overlay traffic and conversion data directly on each canvas node.
/// </summary>
public sealed record NodeStatsDto(
    /// <summary>
    /// Number of user answers recorded at this node.
    /// Meaningful for Question nodes; 0 for InfoPage/Offer nodes.
    /// </summary>
    int AnswerCount,

    /// <summary>
    /// Number of sessions currently positioned at this node
    /// (in-progress or abandoned here).
    /// </summary>
    int DroppedOffCount,

    /// <summary>
    /// Number of times any of this node's linked offers were presented.
    /// Meaningful for Offer nodes; 0 for Question/InfoPage nodes.
    /// </summary>
    int OfferImpressions,

    /// <summary>Number of offer conversions (CTA clicks) at this node.</summary>
    int OfferConversions,

    /// <summary>Offer conversion rate for this node (0–100, 2 dp).</summary>
    double OfferConversionRate,

    /// <summary>
    /// Average time users spent before submitting an answer at this node.
    /// Zero for InfoPage/Offer nodes or nodes with no recorded answers.
    /// </summary>
    TimeSpan AvgAnswerDuration
);

public sealed record OptionDto(
    Guid Id,
    string Label,
    string Value,
    int DisplayOrder,
    string? MediaUrl
);

public sealed record NodeOfferDto(
    Guid Id,
    Guid OfferId,
    bool IsPrimary,
    OfferDto Offer
);

public sealed record EdgeDto(
    Guid Id,
    Guid SourceNodeId,
    Guid TargetNodeId,
    int Priority,
    string? Conditions
);

public sealed record NodeMinimalInfoDto
(
    Guid Id,
    string Type,
    string? AttributeKey,
    string? ValueKind,
    string Title,
    string? AnswerType
);

public sealed record PathDistributionEntryDto(
    string Path,          // raw "nodeId;nodeId;nodeId;" string
    IReadOnlyList<NodeMinimalInfoDto> Nodes,       // parsed for convenience
    int Count,
    int Completed,
    int Abandoned,
    int InProgress
);
