using Application.Contracts.Analytics;

namespace Infrastructure.Contracts.Analytics.Responses;

public sealed record ChannelStatsResponse(List<GlobalChannelStatItem> Items);
