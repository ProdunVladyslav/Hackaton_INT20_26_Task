using Application.Contracts.Analytics;

namespace Infrastructure.Contracts.Analytics.Responses;

public sealed record OfferStatsResponse(List<OfferStatItem> Items);
