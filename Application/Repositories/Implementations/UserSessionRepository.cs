using Application.Contracts.Analytics;
using Application.Repositories.Interfaces;
using Domain.Model.User;
using Microsoft.EntityFrameworkCore;

namespace Application.Repositories.Implementations
{
    public class UserSessionRepository(AppDbContext context) : GenericRepository<UserSession>(context), IUserSessionRepository
    {
        public async Task<UserSession?> GetWithAnswersAsync(Guid sessionId, CancellationToken ct = default)
            => await _context.UserSessions
                .Include(s => s.Answers)
                .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        public async Task<List<DropOffItem>> GetDropOffsByOwnerAsync(Guid userProfileId, CancellationToken ct = default)
        {
            var raw = await _context.UserSessions
                .Where(s => s.Status == SessionStatus.Abandoned && s.CurrentNodeId != null)
                .Join(_context.Nodes, s => s.CurrentNodeId, n => n.Id, (s, n) => new { s, n })
                .Join(_context.Flows, x => x.n.FlowId, f => f.Id, (x, f) => new { x.n, f })
                .Where(x => x.f.OwnerId == userProfileId)
                .GroupBy(x => new
                {
                    NodeId = x.n.Id,
                    NodeTitle = x.n.Title,
                    FlowId = x.f.Id,
                    FlowTitle = x.f.Name
                })
                .Select(g => new
                {
                    g.Key.NodeId,
                    g.Key.NodeTitle,
                    g.Key.FlowId,
                    g.Key.FlowTitle,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync(ct);

            return raw.Select(d => new DropOffItem(
                d.NodeId,
                d.NodeTitle,
                d.FlowId,
                d.FlowTitle,
                d.Count,
                0
            )).ToList();
        }

        public async Task<SessionStatsResponse> GetStatsByOwnerAsync(Guid userProfileId, CancellationToken ct = default)
        {
            var stats = await _context.UserSessions
                .Join(_context.Flows, s => s.FlowId, f => f.Id, (s, f) => new { s, f })
                .Where(x => x.f.OwnerId == userProfileId)
                .GroupBy(_ => true)
                .Select(g => new
                {
                    Total = g.Count(),
                    InProgress = g.Count(x => x.s.Status == SessionStatus.InProgress),
                    Completed = g.Count(x => x.s.Status == SessionStatus.Completed),
                    Abandoned = g.Count(x => x.s.Status == SessionStatus.Abandoned)
                })
                .FirstOrDefaultAsync(ct);

            if (stats is null)
                return new SessionStatsResponse(0, 0, 0, 0, 0, 0);

            double completionRate = stats.Total > 0 ? Math.Round((double)stats.Completed / stats.Total * 100, 2) : 0;
            double abandonRate = stats.Total > 0 ? Math.Round((double)stats.Abandoned / stats.Total * 100, 2) : 0;

            return new SessionStatsResponse(
                stats.Total,
                stats.InProgress,
                stats.Completed,
                stats.Abandoned,
                completionRate,
                abandonRate
            );
        }

        public async Task<int> CountByOwnerAsync(Guid userProfileId, CancellationToken ct = default)
            => await _context.UserSessions
                .Join(_context.Flows, s => s.FlowId, f => f.Id, (s, f) => new { s, f })
                .CountAsync(x => x.f.OwnerId == userProfileId, ct);

        public async Task<FlowSessionStats?> GetSessionStatsByFlowAsync(Guid flowId, CancellationToken ct = default)
            => await _context.UserSessions
                .Where(s => s.FlowId == flowId)
                .GroupBy(_ => 1)
                .Select(g => new FlowSessionStats(
                    g.Count(),
                    g.Count(s => s.Status == SessionStatus.Completed),
                    g.Count(s => s.Status == SessionStatus.Abandoned),
                    g.Count(s => s.Status == SessionStatus.InProgress),
                    (DateTime?)g.Max(s => s.StartedAt)
                ))
                .FirstOrDefaultAsync(ct);

        public async Task<Dictionary<Guid, int>> GetDropOffCountsByFlowAsync(Guid flowId, List<Guid> nodeIds, CancellationToken ct = default)
            => (await _context.UserSessions
                .Where(s => s.FlowId == flowId && s.CurrentNodeId != null && nodeIds.Contains(s.CurrentNodeId.Value))
                .GroupBy(s => s.CurrentNodeId!.Value)
                .Select(g => new { NodeId = g.Key, Count = g.Count() })
                .ToListAsync(ct))
                .ToDictionary(x => x.NodeId, x => x.Count);

        public async Task<List<PathDistributionRaw>> GetPathDistributionAsync(Guid flowId, CancellationToken ct = default)
        {
            var raw = await _context.UserSessions
                .Where(s => s.FlowId == flowId && s.UserNodePath != null)
                .GroupBy(s => s.UserNodePath)
                .Select(g => new
                {
                    Path = g.Key!,
                    Count = g.Count(),
                    Completed = g.Count(s => s.Status == SessionStatus.Completed),
                    Abandoned = g.Count(s => s.Status == SessionStatus.Abandoned),
                    InProgress = g.Count(s => s.Status == SessionStatus.InProgress)
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync(ct);

            return raw.Select(x => new PathDistributionRaw(
                x.Path,
                x.Count,
                x.Completed,
                x.Abandoned,
                x.InProgress
            )).ToList();
        }

        public async Task<Dictionary<Guid, FlowSessionStats>> GetSessionStatsByFlowsAsync(CancellationToken ct = default)
            => (await _context.UserSessions
                .GroupBy(s => s.FlowId)
                .Select(g => new
                {
                    FlowId = g.Key,
                    TotalSessions = g.Count(),
                    CompletedSessions = g.Count(s => s.Status == SessionStatus.Completed),
                    AbandonedSessions = g.Count(s => s.Status == SessionStatus.Abandoned),
                    InProgressSessions = g.Count(s => s.Status == SessionStatus.InProgress),
                    LastSessionAt = (DateTime?)g.Max(s => s.StartedAt),
                })
                .ToListAsync(ct))
                .ToDictionary(x => x.FlowId, x => new FlowSessionStats(
                    x.TotalSessions,
                    x.CompletedSessions,
                    x.AbandonedSessions,
                    x.InProgressSessions,
                    x.LastSessionAt));

        public async Task<Dictionary<Guid, DurationStats>> GetSessionDurationStatsByFlowsAsync(CancellationToken ct = default)
            => (await _context.UserSessions
                .Where(s => s.Status == SessionStatus.Completed && s.CompletedAt != null)
                .Select(s => new { s.FlowId, s.StartedAt, CompletedAt = s.CompletedAt!.Value })
                .ToListAsync(ct))
                .GroupBy(s => s.FlowId)
                .ToDictionary(g => g.Key, g => DurationStatsHelper.Compute(
                    g.Select(s => s.CompletedAt - s.StartedAt)
                     .Where(d => d > TimeSpan.Zero)
                     .OrderBy(d => d)
                     .ToList()));

        public async Task<List<UserSession>> GetByCurrentNodeIdAsync(Guid nodeId, CancellationToken ct = default)
            => await _context.UserSessions
                .Where(s => s.CurrentNodeId == nodeId)
                .ToListAsync(ct);
    }
}
