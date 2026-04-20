using Application.Contracts.Analytics;
using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Analytics;

/// <summary>
/// Use case: Get overall session statistics.
///
/// Returns:
/// - Total sessions
/// - Sessions by status (InProgress, Completed, Abandoned)
/// - Completion rate (%)
/// - Abandon rate (%)
/// </summary>
public sealed class SessionStatsUseCase(
    IUserProfileRepository userProfileRepository,
    IUserSessionRepository userSessionRepository)
{
    public async Task<FlowResult<SessionStatsResponse>> ExecuteAsync(Guid applicationUserId, CancellationToken ct = default)
    {
        var profile = await userProfileRepository.FirstOrDefaultAsync(p => p.ApplicationUserId == applicationUserId, ct);
        if (profile == null)
            return FlowResult<SessionStatsResponse>.NotFound("User profile not found.");

        var stats = await userSessionRepository.GetStatsByOwnerAsync(profile.Id, ct);

        return FlowResult<SessionStatsResponse>.Ok(stats);
    }
}
