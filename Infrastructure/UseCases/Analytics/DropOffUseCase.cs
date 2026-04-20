using Application.Contracts.Analytics;
using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Analytics.Responses;
using Infrastructure.Contracts.Flows.Responses;
namespace Infrastructure.UseCases.Analytics;

/// <summary>
/// Use case: Get drop-off analysis.
///
/// Returns for each node where users abandon:
/// - Node ID and title
/// - Count of sessions still on that node
/// - Drop-off rate (% of total sessions)
/// </summary>
public sealed class DropOffUseCase(
    IUserSessionRepository userSessionRepository,
    IUserProfileRepository userProfileRepository)
{
    public async Task<FlowResult<DropOffResponse>> ExecuteAsync(Guid applicationUserId, CancellationToken ct = default)
    {
        var profile = await userProfileRepository.FirstOrDefaultAsync(p => p.ApplicationUserId == applicationUserId, ct);
        if (profile == null) 
            return FlowResult<DropOffResponse>.NotFound("User profile not found.");

        var total = await userSessionRepository.CountByOwnerAsync(profile.Id, ct);

        List<DropOffItem> items = await userSessionRepository.GetDropOffsByOwnerAsync(profile.Id, ct);

        items = items.Select(d => d with
        {
            DropOffRate = total > 0 ? Math.Round((double)d.SessionCount / total * 100, 2) : 0
        }).ToList();

        return FlowResult<DropOffResponse>.Ok(new DropOffResponse(items));
    }
}
