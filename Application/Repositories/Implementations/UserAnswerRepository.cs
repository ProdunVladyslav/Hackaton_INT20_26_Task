using Application.Contracts.Analytics;
using Application.Repositories.Interfaces;
using Domain.Model.AdminProfile;
using Domain.Model.User;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Repositories.Implementations
{
    public class UserAnswerRepository(AppDbContext context) : GenericRepository<UserAnswer>(context), IUserAnswerRepository
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

            return SessionTimeStats.Compute(session, answers);
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
    }
}
