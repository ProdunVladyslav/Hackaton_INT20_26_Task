using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Analytics.Responses;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Analytics;

/// <summary>
/// Use case: Get lead-quality breakdown (tier distribution + top
/// disqualification reasons) aggregated across every flow the user owns.
/// </summary>
public sealed class LeadQualityUseCase(
    ILeadRepository leadRepository,
    IUserSessionRepository userSessionRepository,
    IUserProfileRepository userProfileRepository)
{
    public async Task<FlowResult<LeadQualityResponse>> ExecuteAsync(Guid applicationUserId, CancellationToken ct = default)
    {
        var profile = await userProfileRepository.FirstOrDefaultAsync(p => p.ApplicationUserId == applicationUserId, ct);
        if (profile == null)
            return FlowResult<LeadQualityResponse>.NotFound("User profile not found.");

        var tierRaw = await leadRepository.GetTierDistributionByOwnerAsync(profile.Id, ct);
        var reasonsRaw = await userSessionRepository.GetDisqualificationReasonsByOwnerAsync(profile.Id, ct);

        var totalLeads = tierRaw.Sum(r => r.Count);
        var tierDistribution = tierRaw
            .OrderByDescending(r => r.Count)
            .Select(r => new TierDistributionEntryDto(r.Tier, r.Count, Rate(r.Count, totalLeads)))
            .ToList();

        var totalDisqualified = reasonsRaw.Sum(r => r.Count);
        var disqualBreakdown = reasonsRaw
            .OrderByDescending(r => r.Count)
            .Select(r => new DisqualificationReasonDto(r.Reason, r.Count, Rate(r.Count, totalDisqualified)))
            .ToList();

        return FlowResult<LeadQualityResponse>.Ok(new LeadQualityResponse(tierDistribution, disqualBreakdown));
    }

    // Matches FlowStatsQueryService's Rate() convention — 0.0–1.0, 4 dp — since
    // these DTOs are consumed by the same frontend pct() formatter.
    private static double Rate(int num, int den) => den > 0 ? Math.Round((double)num / den, 4) : 0d;
}
