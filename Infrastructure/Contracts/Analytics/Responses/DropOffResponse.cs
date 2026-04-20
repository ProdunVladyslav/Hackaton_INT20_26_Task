using Application.Contracts.Analytics;

namespace Infrastructure.Contracts.Analytics.Responses;

public sealed record DropOffResponse(List<DropOffItem> Items);
