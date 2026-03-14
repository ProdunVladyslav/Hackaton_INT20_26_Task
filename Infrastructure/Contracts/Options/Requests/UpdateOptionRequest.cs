namespace Infrastructure.Contracts.Options.Requests;

public sealed record UpdateOptionRequest(
    string? Label,
    string? Value,
    int? DisplayOrder,
    string? MediaUrl
);
