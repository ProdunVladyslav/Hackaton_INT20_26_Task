using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Analytics.Responses;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Analytics;

/// <summary>
/// Use case: Get lead-channel performance across every flow the user owns.
/// </summary>
public sealed class ChannelStatsUseCase(
    ILeadChannelRepository leadChannelRepository,
    IUserProfileRepository userProfileRepository)
{
    public async Task<FlowResult<ChannelStatsResponse>> ExecuteAsync(Guid applicationUserId, CancellationToken ct = default)
    {
        var profile = await userProfileRepository.FirstOrDefaultAsync(p => p.ApplicationUserId == applicationUserId, ct);
        if (profile == null)
            return FlowResult<ChannelStatsResponse>.NotFound("User profile not found.");

        var items = await leadChannelRepository.GetStatsByOwnerAsync(profile.Id, ct);

        return FlowResult<ChannelStatsResponse>.Ok(new ChannelStatsResponse(items));
    }
}
