namespace Infrastructure.Contracts.Nodes.Responses;

public sealed record NodeResponse(
    Guid Id,
    Guid FlowId,
    string Type,
    string? AttributeKey,
    string Title,
    string? Description,
    string? MediaUrl,
    float PositionX,
    float PositionY,
    DateTime CreatedAt
);
