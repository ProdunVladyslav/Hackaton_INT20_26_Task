using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.Contracts.Analytics.Responses;

public sealed record LeadQualityResponse(
    List<TierDistributionEntryDto> TierDistribution,
    List<DisqualificationReasonDto> DisqualificationBreakdown
);
