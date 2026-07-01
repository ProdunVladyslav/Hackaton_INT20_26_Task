using Application.Contracts.Analytics;
using Application.Repositories.Interfaces;
using Domain.Model.AdminProfile;
using Domain.Model.Survey;
using Domain.Model.User;
using Domain.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Repositories.Implementations
{
    public class UserAnswerRepository(
        AppDbContext context,
        IDateTimeProvider time) : GenericRepository<UserAnswer>(context), IUserAnswerRepository
    {
        public async Task<List<UserAnswer>> GetBySessionOrderedAsync(Guid sessionId, CancellationToken ct = default)
            => await _context.UserAnswers.Where(a => a.SessionId == sessionId).OrderBy(a => a.AnsweredAt).ToListAsync(ct);

        public async Task<DateTime?> GetLastAnsweredAtAsync(Guid sessionId, CancellationToken ct = default)
            => await _context.UserAnswers
                .Where(a => a.SessionId == sessionId)
                .OrderByDescending(a => a.AnsweredAt)
                .Select(a => (DateTime?)a.AnsweredAt)
                .FirstOrDefaultAsync(ct);

        public async Task<SessionTimeStats> GetTimeStatsAsync(Guid sessionId, CancellationToken ct = default)
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId, ct)
                ?? throw new InvalidOperationException($"Session {sessionId} not found");

            var answers = await _context.UserAnswers
                .Where(a => a.SessionId == sessionId)
                .OrderBy(a => a.AnsweredAt)
                .ToListAsync(ct);

            return SessionTimeStats.Compute(session, answers, time);
        }

        public async Task<FlowTimeStats> GetFlowTimeStatsAsync(Guid flowId, CancellationToken ct = default)
        {
            var sessions = await _context.UserSessions
                .Where(s => s.FlowId == flowId)
                .ToListAsync(ct);

            var sessionIds = sessions.Select(s => s.Id).ToList();

            // Materialise all answers first — no TimeSpan filters in SQL
            var answers = await _context.UserAnswers
                .Where(a => sessionIds.Contains(a.SessionId))
                .ToListAsync(ct);

            // TimeSpan.Zero guard happens inside FlowTimeStats.Compute (pure C#)
            return FlowTimeStats.Compute(sessions, answers);
        }

        public async Task<Dictionary<Guid, int>> GetAnswerCountsByNodeIdsAsync(List<Guid> nodeIds, CancellationToken ct = default)
            => (await _context.UserAnswers
                .Where(a => nodeIds.Contains(a.NodeId))
                .GroupBy(a => a.NodeId)
                .Select(g => new { NodeId = g.Key, Count = g.Count() })
                .ToListAsync(ct))
                .ToDictionary(x => x.NodeId, x => x.Count);

        public async Task<Dictionary<Guid, DurationStats>> GetAnswerDurationStatsByFlowsAsync(CancellationToken ct = default)
            => (await _context.UserAnswers
                .Join(_context.UserSessions, a => a.SessionId, s => s.Id, (a, s) => new { s.FlowId, a.UserAnswerDuration })
                .ToListAsync(ct))
                .Where(x => x.UserAnswerDuration > TimeSpan.Zero)
                .GroupBy(x => x.FlowId)
                .ToDictionary(g => g.Key, g => DurationStatsHelper.Compute(
                    g.Select(x => x.UserAnswerDuration)
                     .OrderBy(d => d)
                     .ToList()));

        public async Task<List<UserAnswer>> GetBySessionAsync(Guid sessionId, CancellationToken ct = default)
            => await _context.UserAnswers
                .Where(a => a.SessionId == sessionId)
                .ToListAsync(ct);

        public async Task<decimal?> ResolveNumericValueAsync(string storedValue, CancellationToken ct = default)
        {
            if (decimal.TryParse(storedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var direct))
                return direct;

            var option = await _context.Options.FirstOrDefaultAsync(o => o.Value == storedValue, ct);
            if (option == null) return null;

            return decimal.TryParse(option.Label, NumberStyles.Any, CultureInfo.InvariantCulture, out var labelNum)
                ? labelNum : null;
        }

        public async Task DeleteByNodeIdAsync(Guid nodeId, CancellationToken ct = default)
            => await _context.UserAnswers
                .Where(a => a.NodeId == nodeId)
                .ExecuteDeleteAsync(ct);

        public async Task<Dictionary<Guid, List<AnswerOptionStats>>> GetAnswerDistributionByNodeIdsAsync(
            List<Guid> nodeIds, CancellationToken ct = default)
        {
            // Total answers per node (for Share calculation)
            var totals = await _context.UserAnswers
                .Where(a => nodeIds.Contains(a.NodeId))
                .GroupBy(a => a.NodeId)
                .Select(g => new { NodeId = g.Key, Total = g.Count() })
                .ToDictionaryAsync(x => x.NodeId, x => x.Total, ct);

            // Per-value counts + qualification rate
            var rows = await _context.UserAnswers
                .Where(a => nodeIds.Contains(a.NodeId))
                .GroupBy(a => new { a.NodeId, a.Value })
                .Select(g => new
                {
                    g.Key.NodeId,
                    g.Key.Value,
                    Count = g.Count(),
                    // QualificationRate: of sessions that gave this answer,
                    // how many completed at an Offer node
                    QualifiedCount = g
                        .Select(a => a.SessionId)
                        .Distinct()
                        .Count(sid => _context.UserSessions.Any(s =>
                            s.Id == sid &&
                            s.Status == SessionStatus.Completed &&
                            _context.Nodes.Any(n =>
                                n.Id == s.CurrentNodeId &&
                                n.Type == NodeType.Offer))),
                    SessionCount = g.Select(a => a.SessionId).Distinct().Count(),
                })
                .ToListAsync(ct);

            // Resolve labels from Options table
            var allValues = rows.Select(r => r.Value).Distinct().ToList();
            var labelMap = await _context.Options
                .Where(o => allValues.Contains(o.Value))
                .GroupBy(o => o.Value)
                .Select(g => new { Value = g.Key, Label = g.First().Label })
                .ToDictionaryAsync(x => x.Value, x => x.Label, ct);

            return rows
                .GroupBy(r => r.NodeId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var total = totals.GetValueOrDefault(g.Key, 1);
                        return g.Select(r => new AnswerOptionStats(
                            Value: r.Value,
                            Label: labelMap.GetValueOrDefault(r.Value, r.Value),
                            Count: r.Count,
                            Share: Math.Round((double)r.Count / total, 4),
                            QualificationRate: r.SessionCount > 0
                                ? Math.Round((double)r.QualifiedCount / r.SessionCount, 4) : 0d,
                            AvgScore: null  // populate if you store per-answer scores
                        )).ToList();
                    });
        }

        public async Task<Dictionary<Guid, List<TopTextAnswerRaw>>> GetTopTextAnswersByNodeIdsAsync(
            List<Guid> nodeIds, int topN, CancellationToken ct = default)
        {
            var rows = await _context.UserAnswers
                .Where(a => nodeIds.Contains(a.NodeId))
                .GroupBy(a => new { a.NodeId, NormalisedValue = a.Value.Trim().ToLower() })
                .Select(g => new
                {
                    g.Key.NodeId,
                    Value = g.Key.NormalisedValue,
                    Count = g.Count()
                })
                .ToListAsync(ct);

            return rows
                .GroupBy(r => r.NodeId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(r => r.Count)
                          .Take(topN)
                          .Select(r => new TopTextAnswerRaw(r.Value, r.Count))
                          .ToList());
        }

        public async Task<Dictionary<Guid, int>> GetAvgAnswerSecondsByNodeIdsAsync(
            List<Guid> nodeIds, CancellationToken ct = default)
        {
            return await _context.UserAnswers
                .Where(a => nodeIds.Contains(a.NodeId))
                .GroupBy(a => a.NodeId)
                .Select(g => new
                {
                    NodeId = g.Key,
                    AvgSeconds = (int)g.Average(a => a.UserAnswerDuration.TotalSeconds)
                })
                .ToDictionaryAsync(x => x.NodeId, x => x.AvgSeconds, ct);
        }
    }
}
