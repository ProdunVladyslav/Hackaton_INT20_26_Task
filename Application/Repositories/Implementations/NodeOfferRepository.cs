using Application.Contracts.Quiz;
using Application.Repositories.Interfaces;
using Domain.Model.AdminProfile;
using Domain.Model.Survey;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Repositories.Implementations
{
    public class NodeOfferRepository(AppDbContext context) : GenericRepository<NodeOffer>(context), INodeOfferRepository
    {
        public async Task<List<(NodeOffer Link, Offer Offer)>> GetByNodeIdWithOfferAsync(Guid nodeId, CancellationToken ct = default)
        {
            return await _context.NodeOffers
                .Where(no => no.NodeId == nodeId)
                .Join(_context.Offers, no => no.OfferId, o => o.Id, (no, o) => new ValueTuple<NodeOffer, Offer>(no, o))
                .ToListAsync(ct);
        }

        public async Task<Dictionary<Guid, List<NodeOffer>>> GetByNodeIdsWithOffersAsync(List<Guid> nodeIds, CancellationToken ct = default)
            => (await _context.NodeOffers
                .Where(no => nodeIds.Contains(no.NodeId))
                .Include(no => no.Offer)
                .ToListAsync(ct))
                .GroupBy(no => no.NodeId)
                .ToDictionary(g => g.Key, g => g.ToList());

        public async Task DeleteByNodeIdAsync(Guid nodeId, CancellationToken ct = default)
        {
            var nodeOffers = await _context.NodeOffers.Where(no => no.NodeId == nodeId).ToListAsync(ct);
            _context.NodeOffers.RemoveRange(nodeOffers);
        }

        public async Task<List<NodeOfferWithOffer>> GetByNodeIdWithOfferForQuizAsync(Guid nodeId, CancellationToken ct = default)
        {
            var raw = await _context.NodeOffers
                .Where(no => no.NodeId == nodeId)
                .Join(_context.Offers, no => no.OfferId, o => o.Id, (no, o) => new { no, o })
                .ToListAsync(ct);

            return raw.Select(x => new NodeOfferWithOffer(x.no, x.o)).ToList();
        }

        public async Task<List<NodeOffer>> GetByNodeIdAsync(Guid nodeId, CancellationToken ct = default)
            => await _context.NodeOffers
                .Where(no => no.NodeId == nodeId)
                .ToListAsync(ct);
    }
}
