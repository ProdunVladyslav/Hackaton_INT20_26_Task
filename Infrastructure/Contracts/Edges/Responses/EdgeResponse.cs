namespace Infrastructure.Contracts.Edges.Responses;

public sealed record EdgeResponse(
    Guid Id,
    Guid FlowId,
    Guid SourceNodeId,
    Guid TargetNodeId,
    int Priority,
    string? ConditionsJson,
    DateTime CreatedAt
);
