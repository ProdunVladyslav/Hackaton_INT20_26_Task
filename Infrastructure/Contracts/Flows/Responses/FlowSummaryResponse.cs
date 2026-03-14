namespace Infrastructure.Contracts.Flows.Responses;

/// <summary>
/// Lightweight flow representation for list endpoints.
/// </summary>
public sealed record FlowSummaryResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsPublished,
    Guid? EntryNodeId,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
