using Application.Contracts.Analytics;
using Application.Repositories.Interfaces;
using Domain.Model.Survey;
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

        public async Task<FlowSessionStats?> GetSessionStatsByFlowAsync(
            Guid flowId, CancellationToken ct = default)
            => await _context.UserSessions
                .Where(s => s.FlowId == flowId)
                .GroupBy(_ => 1)
                .Select(g => new FlowSessionStats(
                    g.Count(),
                    g.Count(s => s.Status == SessionStatus.Completed),
                    g.Count(s => s.Status == SessionStatus.Abandoned),
                    g.Count(s => s.TerminalNodeType == "Offer"),
                    g.Count(s => s.TerminalNodeType == "Redirect"),
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

        public async Task<List<PathDistributionRaw>> GetPathDistributionAsync(
            Guid flowId, CancellationToken ct = default)
        {
            var paths = await _context.UserSessions
                .Where(s => s.FlowId == flowId && s.UserNodePath != null)
                .GroupBy(s => s.UserNodePath!)
                .Select(g => new
                {
                    Path = g.Key,
                    Count = g.Count(),
                    Completed = g.Count(s => s.Status == SessionStatus.Completed),
                    Abandoned = g.Count(s => s.Status == SessionStatus.Abandoned),
                    InProgress = g.Count(s => s.Status == SessionStatus.InProgress),
                })
                .ToListAsync(ct);

            // Resolve terminal node type and converted count per path
            var result = new List<PathDistributionRaw>();

            foreach (var p in paths)
            {
                var lastNodeIdStr = p.Path
                    .Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .LastOrDefault();

                string? terminalType = null;
                if (Guid.TryParse(lastNodeIdStr, out var lastNodeId))
                {
                    var nodeType = await _context.Nodes
                        .Where(n => n.Id == lastNodeId)
                        .Select(n => (NodeType?)n.Type)
                        .FirstOrDefaultAsync(ct);

                    terminalType = nodeType?.ToString(); // "Offer" | "Redirect" | null
                }

                var converted = await _context.SessionOffers
                    .CountAsync(o => o.Converted &&
                        _context.UserSessions.Any(s =>
                            s.Id == o.SessionId &&
                            s.UserNodePath == p.Path), ct);

                var qualified = terminalType == nameof(NodeType.Offer) ? p.Completed : 0;

                result.Add(new PathDistributionRaw(
                    Path: p.Path,
                    Count: p.Count,
                    Completed: p.Completed,
                    Qualified: qualified,
                    Abandoned: p.Abandoned,
                    InProgress: p.InProgress,
                    Converted: converted,
                    TerminalNodeType: terminalType
                ));
            }

            return result;
        }

        public async Task<Dictionary<Guid, FlowSessionStats>> GetSessionStatsByFlowsAsync(
            CancellationToken ct = default)
            => (await _context.UserSessions
                .GroupBy(s => s.FlowId)
                .Select(g => new
                {
                    FlowId = g.Key,
                    TotalSessions = g.Count(),
                    CompletedSessions = g.Count(s => s.Status == SessionStatus.Completed),
                    AbandonedSessions = g.Count(s => s.Status == SessionStatus.Abandoned),
                    QualifiedSessions = g.Count(s => s.TerminalNodeType == "Offer"),
                    DisqualifiedSessions = g.Count(s => s.TerminalNodeType == "Redirect"),
                    InProgressSessions = g.Count(s => s.Status == SessionStatus.InProgress),
                    LastSessionAt = (DateTime?)g.Max(s => s.StartedAt),
                })
                .ToListAsync(ct))
                .ToDictionary(x => x.FlowId, x => new FlowSessionStats(
                    TotalSessions: x.TotalSessions,
                    CompletedSessions: x.CompletedSessions,
                    AbandonedSessions: x.AbandonedSessions,
                    QualifiedSessions: x.QualifiedSessions,
                    DisqualifiedSessions: x.DisqualifiedSessions,
                    InProgressSessions: x.InProgressSessions,
                    LastSessionAt: x.LastSessionAt));

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

        public async Task<List<DailySessionStats>> GetDailySeriesAsync(
    Guid flowId, DateOnly from, DateOnly to, CancellationToken ct = default)
        {
            // Pull raw per-day counts from DB
            var raw = await _context.UserSessions
                .Where(s => s.FlowId == flowId
                         && DateOnly.FromDateTime(s.StartedAt) >= from
                         && DateOnly.FromDateTime(s.StartedAt) <= to)
                .GroupBy(s => DateOnly.FromDateTime(s.StartedAt))
                .Select(g => new
                {
                    Date = g.Key,
                    Started = g.Count(),
                    Completed = g.Count(s => s.Status == SessionStatus.Completed),
                    // Qualified = completed at an Offer node (CurrentNodeId resolves to Offer)
                    // We store terminal node type on the session — if you don't have it yet,
                    // use a join to Nodes. For now: sessions where CurrentNodeId is an Offer node.
                    Qualified = g.Count(s =>
                        s.Status == SessionStatus.Completed &&
                        _context.Nodes.Any(n => n.Id == s.CurrentNodeId
                                             && n.Type == NodeType.Offer)),
                })
                .ToListAsync(ct);

            // Fill in days with zero activity so the chart line is continuous
            var lookup = raw.ToDictionary(r => r.Date);
            var result = new List<DailySessionStats>();

            for (var d = from; d <= to; d = d.AddDays(1))
            {
                if (lookup.TryGetValue(d, out var row))
                {
                    // Converted = sessions that also have a SessionOffer with Converted=true
                    var converted = await _context.SessionOffers
                        .CountAsync(o => o.Converted &&
                            _context.UserSessions.Any(s =>
                                s.Id == o.SessionId &&
                                s.FlowId == flowId &&
                                DateOnly.FromDateTime(s.StartedAt) == d), ct);

                    result.Add(new DailySessionStats(d,
                        row.Started, row.Completed, row.Qualified, converted));
                }
                else
                {
                    result.Add(new DailySessionStats(d, 0, 0, 0, 0));
                }
            }

            return result;
        }

        public async Task<List<DisqualificationReasonRaw>> GetDisqualificationReasonsAsync(
            Guid flowId, CancellationToken ct = default)
        {
            var reasons = await _context.UserSessions
                .Where(s => s.FlowId == flowId && s.Status == SessionStatus.Completed)
                .Join(_context.NodeRedirects,
                    s => s.CurrentNodeId,
                    r => r.NodeId,
                    (s, r) => r.DisqualificationReason)
                .Where(reason => reason != null)
                .ToListAsync(ct);  // ← pull to client here, then group in memory

            return reasons
                .GroupBy(reason => reason!)
                .Select(g => new DisqualificationReasonRaw(g.Key, g.Count()))
                .OrderByDescending(x => x.Count)
                .ToList();
        }

        public async Task<ScoreDistributionRaw?> GetScoreDistributionAsync(
            Guid flowId, CancellationToken ct = default)
        {
            var scores = await _context.UserSessions
                .Where(s => s.FlowId == flowId && s.Status == SessionStatus.Completed)
                .Select(s => (double)s.Score)
                .ToListAsync(ct);

            if (scores.Count == 0) return null;

            scores.Sort();
            var min = scores.First();
            var max = scores.Last();
            var avg = scores.Average();
            var median = scores.Count % 2 == 0
                ? (scores[scores.Count / 2 - 1] + scores[scores.Count / 2]) / 2.0
                : scores[scores.Count / 2];

            // 10 equal-width buckets
            const int bucketCount = 10;
            var width = (max - min) == 0 ? 1 : (max - min) / bucketCount;
            var buckets = Enumerable.Range(0, bucketCount).Select(i =>
            {
                var from = min + i * width;
                var to = i == bucketCount - 1 ? max : from + width;
                var count = scores.Count(s => s >= from && (i == bucketCount - 1 ? s <= to : s < to));
                return new ScoreBucketRaw(from, to, count);
            }).ToList();

            return new ScoreDistributionRaw(min, max, avg, median,
                QualificationThreshold: null,  // wire up when Flow gets a threshold field
                Buckets: buckets);
        }
    }
}
