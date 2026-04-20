namespace Infrastructure.Contracts.Options.Requests;

public sealed record UpdateOptionRequest(
    string? Label = null,
    string? Value = null,
    int? DisplayOrder = null,
    string? MediaUrl = null,
    int? ScoreDelta = null
);