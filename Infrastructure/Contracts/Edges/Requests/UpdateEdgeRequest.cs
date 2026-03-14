namespace Infrastructure.Contracts.Edges.Requests;

public sealed record UpdateEdgeRequest(int? Priority, string? ConditionsJson);
