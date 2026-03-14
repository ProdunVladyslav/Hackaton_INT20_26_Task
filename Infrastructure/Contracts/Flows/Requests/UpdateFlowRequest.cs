namespace Infrastructure.Contracts.Flows.Requests;

public sealed record UpdateFlowRequest(
    string? Name,
    string? Description
);
