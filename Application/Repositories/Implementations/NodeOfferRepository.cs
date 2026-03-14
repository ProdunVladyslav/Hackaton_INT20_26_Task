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
    }
}
