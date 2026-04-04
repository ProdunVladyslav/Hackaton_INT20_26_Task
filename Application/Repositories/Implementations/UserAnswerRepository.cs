using Application.Repositories.Interfaces;
using Domain.Model.AdminProfile;
using Domain.Model.User;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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
    }
}
