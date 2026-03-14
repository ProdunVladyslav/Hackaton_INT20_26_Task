namespace Infrastructure.Contracts.Nodes.Requests;

public sealed record UpdateNodeRequest(
    string? Title,
    string? AttributeKey,
    string? Description,
    string? MediaUrl
);
