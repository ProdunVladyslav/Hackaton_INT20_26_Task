namespace Infrastructure.Contracts.Flows.Responses;

/// <summary>
/// Full flow with all nodes, edges, options, and node-offer links.
/// Used by the admin visual editor to render the entire DAG on canvas.
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
    List<EdgeDto> Edges
);

public sealed record NodeDto(
    Guid Id,
    string Type,
    string? AttributeKey,
    string Title,
    string? Description,
    string? MediaUrl,
    float PositionX,
    float PositionY,
    DateTime CreatedAt,
    List<OptionDto> Options,
    List<NodeOfferDto> NodeOffers
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
    bool IsPrimary
);

public sealed record EdgeDto(
    Guid Id,
    Guid SourceNodeId,
    Guid TargetNodeId,
    int Priority,
    string? Conditions
);
