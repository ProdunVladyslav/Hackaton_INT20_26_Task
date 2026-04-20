using Application.Contracts.Analytics;
using Application.Repositories.Interfaces;
using Domain.Model.User;
using Microsoft.EntityFrameworkCore;

namespace Application.Repositories.Implementations
{
    public class SessionOfferRepository(AppDbContext context) : GenericRepository<SessionOffer>(context), ISessionOfferRepository
    {
        public async Task<List<SessionOffer>> GetBySessionAsync(Guid sessionId, CancellationToken ct = default)
            => await _context.SessionOffers.Where(so => so.SessionId == sessionId).ToListAsync(ct);

        public async Task<SessionOffer?> GetBySessionAndOfferAsync(Guid sessionId, Guid offerId, CancellationToken ct = default)
            => await _context.SessionOffers.FirstOrDefaultAsync(so => so.SessionId == sessionId && so.OfferId == offerId, ct);

        public async Task<List<OfferStatItem>> GetOfferStatsByOwnerAsync(Guid userProfileId, CancellationToken ct = default)
        {
            var raw = await _context.SessionOffers
                .Join(_context.UserSessions, so => so.SessionId, s => s.Id, (so, s) => new { so, s })
                .Join(_context.Offers, x => x.so.OfferId, o => o.Id, (x, o) => new { x.so, x.s, o })
                .Join(_context.Flows, x => x.s.FlowId, f => f.Id, (x, f) => new { x.so, x.o, f })
                .Where(x => x.f.OwnerId == userProfileId)
                .GroupBy(x => new
                {
                    OfferId = x.o.Id,
                    OfferName = x.o.Name,
                    OfferSlug = x.o.Slug,
                    FlowId = x.f.Id,
                    FlowName = x.f.Name
                })
                .Select(g => new
                {
                    g.Key.OfferId,
                    g.Key.OfferName,
                    g.Key.OfferSlug,
                    g.Key.FlowId,
                    g.Key.FlowName,
                    TimesPresented = g.Count(),
                    TimesConverted = g.Count(x => x.so.Converted)
                })
                .OrderByDescending(x => x.TimesPresented)
                .ToListAsync(ct); // ← materialize here, before any C# logic

            // Math.Round and record constructors run in memory, not in SQL
            return raw.Select(x => new OfferStatItem(
                x.OfferId,
                x.OfferName,
                x.OfferSlug,
                x.FlowId,
                x.FlowName,
                x.TimesPresented,
                x.TimesConverted,
                x.TimesPresented > 0
                    ? Math.Round((double)x.TimesConverted / x.TimesPresented * 100, 2)
                    : 0
            )).ToList();
        }

        public async Task<FlowOfferStats?> GetOfferStatsByFlowAsync(Guid flowId, CancellationToken ct = default)
            => await _context.SessionOffers
                .Join(_context.UserSessions, so => so.SessionId, s => s.Id, (so, s) => new { so.Converted, s.FlowId })
                .Where(x => x.FlowId == flowId)
                .GroupBy(_ => 1)
                .Select(g => new FlowOfferStats(g.Count(), g.Count(x => x.Converted)))
                .FirstOrDefaultAsync(ct);

        public async Task<Dictionary<Guid, NodeImpressionStats>> GetNodeImpressionsByFlowAsync(Guid flowId, List<Guid> nodeIds, CancellationToken ct = default)
            => (await (
                from no in _context.NodeOffers
                where nodeIds.Contains(no.NodeId)
                join so in _context.SessionOffers on no.OfferId equals so.OfferId
                join s in _context.UserSessions on so.SessionId equals s.Id
                where s.FlowId == flowId
                group new { so.Converted } by no.NodeId into g
                select new { NodeId = g.Key, Stats = new NodeImpressionStats(g.Count(), g.Count(x => x.Converted)) }
            ).ToListAsync(ct))
            .ToDictionary(x => x.NodeId, x => x.Stats);

        public async Task<Dictionary<Guid, FlowOfferStats>> GetOfferStatsByFlowsAsync(CancellationToken ct = default)
            => (await _context.SessionOffers
                .Join(_context.UserSessions, so => so.SessionId, s => s.Id, (so, s) => new { so.Converted, s.FlowId })
                .GroupBy(x => x.FlowId)
                .Select(g => new
                {
                    FlowId = g.Key,
                    TotalImpressions = g.Count(),
                    TotalConversions = g.Count(x => x.Converted),
                })
                .ToListAsync(ct))
                .ToDictionary(x => x.FlowId, x => new FlowOfferStats(x.TotalImpressions, x.TotalConversions));
    }
}
