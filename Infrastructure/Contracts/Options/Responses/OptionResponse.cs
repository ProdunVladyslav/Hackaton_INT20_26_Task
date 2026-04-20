namespace Infrastructure.Contracts.Options.Responses;

public sealed record OptionResponse(
    Guid Id,
    Guid NodeId,
    string Label,
    string Value,
    int DisplayOrder,
    string? MediaUrl,
    int ScoreDelta
);