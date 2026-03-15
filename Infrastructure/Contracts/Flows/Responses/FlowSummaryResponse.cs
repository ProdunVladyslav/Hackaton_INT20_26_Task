namespace Infrastructure.Contracts.Flows.Responses;

/// <summary>
/// Lightweight flow representation for list endpoints.
/// <see cref="Stats"/> is populated on admin routes and null on public routes.
/// </summary>
public sealed record FlowSummaryResponse(
    Guid            Id,
    string          Name,
    string?         Description,
    bool            IsPublished,
    Guid?           EntryNodeId,
    DateTime        CreatedAt,
    DateTime        UpdatedAt,
    FlowAdminStats? Stats
);
