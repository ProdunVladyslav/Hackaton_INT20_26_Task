using Application;
using Domain.Model.User;
using Infrastructure.Contracts.Analytics.Responses;
using Infrastructure.Contracts.Flows.Responses;
using Microsoft.EntityFrameworkCore;

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
public sealed class SessionStatsUseCase
{
    private readonly AppDbContext _db;

    public SessionStatsUseCase(AppDbContext db) => _db = db;

    public async Task<FlowResult<SessionStatsResponse>> ExecuteAsync(CancellationToken ct = default)
    {
        var stats = await _db.UserSessions
            .GroupBy(_ => true)
            .Select(g => new
            {
                Total = g.Count(),
                InProgress = g.Count(s => s.Status == SessionStatus.InProgress),
                Completed = g.Count(s => s.Status == SessionStatus.Completed),
                Abandoned = g.Count(s => s.Status == SessionStatus.Abandoned)
            })
            .FirstOrDefaultAsync(ct);

        if (stats is null)
            return FlowResult<SessionStatsResponse>.Ok(new SessionStatsResponse(0, 0, 0, 0, 0, 0));

        double completionRate = stats.Total > 0 ? Math.Round((double)stats.Completed / stats.Total * 100, 2) : 0;
        double abandonRate = stats.Total > 0 ? Math.Round((double)stats.Abandoned / stats.Total * 100, 2) : 0;

        return FlowResult<SessionStatsResponse>.Ok(new SessionStatsResponse(
            stats.Total,
            stats.InProgress,
            stats.Completed,
            stats.Abandoned,
            completionRate,
            abandonRate
        ));
    }
}
