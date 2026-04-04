namespace Infrastructure.Contracts.Analytics.Responses;

public sealed record DropOffResponse(List<DropOffItem> Items);

public sealed record DropOffItem(Guid NodeId, string NodeTitle, Guid FlowId, string FlowTitle, int SessionCount, double DropOffRate);
