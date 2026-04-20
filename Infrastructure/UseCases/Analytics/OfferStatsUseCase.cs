using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Analytics.Responses;
using Infrastructure.Contracts.Flows.Responses;
namespace Infrastructure.UseCases.Analytics;

/// <summary>
/// Use case: Get offer performance statistics.
///
/// Returns per-offer metrics:
/// - Times presented
/// - Times converted
/// - Conversion rate (%)
/// </summary>
public sealed class OfferStatsUseCase(
    ISessionOfferRepository sessionOfferRepository,
    IUserProfileRepository userProfileRepository)
{
    public async Task<FlowResult<OfferStatsResponse>> ExecuteAsync(Guid applicationUserId, CancellationToken ct = default)
    {
        var profile = await userProfileRepository.FirstOrDefaultAsync(p => p.ApplicationUserId == applicationUserId, ct);
        if (profile == null)
            return FlowResult<OfferStatsResponse>.NotFound("User profile not found.");

        var items = await sessionOfferRepository.GetOfferStatsByOwnerAsync(profile.Id, ct);

        return FlowResult<OfferStatsResponse>.Ok(new OfferStatsResponse(items));
    }
}