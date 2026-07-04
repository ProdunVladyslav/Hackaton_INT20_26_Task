using Application.Contracts.Analytics;
using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Domain.Model.User;
using Microsoft.EntityFrameworkCore;

namespace Application.Repositories.Implementations
{
    public class LeadChannelRepository(AppDbContext context)
        : GenericRepository<LeadChannel>(context), ILeadChannelRepository
    {
        public async Task<LeadChannel?> GetByShortCodeAsync(string shortCode, CancellationToken ct = default)
            => await _context.LeadChannels
                .FirstOrDefaultAsync(c => c.ShortCode == shortCode, ct);

        public async Task<bool> ExistsByShortCodeAsync(string shortCode, CancellationToken ct = default)
            => await _context.LeadChannels
                .AnyAsync(c => c.ShortCode == shortCode, ct);

        public async Task<List<LeadChannel>> GetByFlowIdAsync(Guid flowId, CancellationToken ct = default)
            => await _context.LeadChannels
                .Where(c => c.FlowId == flowId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync(ct);

        public async Task<Dictionary<Guid, LeadChannelStats>> GetStatsByFlowAsync(
            Guid flowId, CancellationToken ct = default)
        {
            var rows = await _context.UserSessions
                .Where(s => s.FlowId == flowId && s.LeadChannelId != null)
                .GroupBy(s => s.LeadChannelId!.Value)
                .Select(g => new
                {
                    LeadChannelId = g.Key,
                    SessionCount = g.Count(),
                    QualifiedCount = g.Count(s => s.TerminalNodeType == "Offer"),
                    DisqualifiedCount = g.Count(s => s.TerminalNodeType == "Redirect"),
                })
                .ToListAsync(ct);

            return rows.ToDictionary(
                r => r.LeadChannelId,
                r => new LeadChannelStats(r.SessionCount, r.QualifiedCount, r.DisqualifiedCount));
        }

        public async Task<List<GlobalChannelStatItem>> GetStatsByOwnerAsync(
            Guid userProfileId, CancellationToken ct = default)
        {
            var channels = await _context.LeadChannels
                .Join(_context.Flows, c => c.FlowId, f => f.Id, (c, f) => new { c, f })
                .Where(x => x.f.OwnerId == userProfileId)
                .Select(x => new
                {
                    x.c.Id,
                    x.c.Name,
                    x.c.ShortCode,
                    x.c.IsArchived,
                    FlowId = x.f.Id,
                    FlowName = x.f.Name
                })
                .ToListAsync(ct);

            if (channels.Count == 0) return new List<GlobalChannelStatItem>();

            var channelIds = channels.Select(c => c.Id).ToList();

            var statsRows = await _context.UserSessions
                .Where(s => s.LeadChannelId != null && channelIds.Contains(s.LeadChannelId.Value))
                .GroupBy(s => s.LeadChannelId!.Value)
                .Select(g => new
                {
                    LeadChannelId = g.Key,
                    SessionCount = g.Count(),
                    QualifiedCount = g.Count(s => s.TerminalNodeType == "Offer"),
                    DisqualifiedCount = g.Count(s => s.TerminalNodeType == "Redirect"),
                })
                .ToListAsync(ct);

            var statsMap = statsRows.ToDictionary(r => r.LeadChannelId);

            return channels.Select(c =>
            {
                statsMap.TryGetValue(c.Id, out var s);
                var sessions = s?.SessionCount ?? 0;
                var qualified = s?.QualifiedCount ?? 0;

                return new GlobalChannelStatItem(
                    LeadChannelId: c.Id,
                    Name: c.Name,
                    ShortCode: c.ShortCode,
                    IsArchived: c.IsArchived,
                    FlowId: c.FlowId,
                    FlowName: c.FlowName,
                    Sessions: sessions,
                    Qualified: qualified,
                    Disqualified: s?.DisqualifiedCount ?? 0,
                    QualificationRate: sessions > 0 ? Math.Round((double)qualified / sessions, 4) : 0d);
            })
            .OrderByDescending(c => c.Sessions)
            .ToList();
        }
    }
}
