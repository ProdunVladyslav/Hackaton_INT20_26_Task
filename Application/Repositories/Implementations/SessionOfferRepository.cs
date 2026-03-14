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
    public class SessionOfferRepository(AppDbContext context) : GenericRepository<SessionOffer>(context), ISessionOfferRepository
    {
        public async Task<List<SessionOffer>> GetBySessionAsync(Guid sessionId, CancellationToken ct = default)
            => await _context.SessionOffers.Where(so => so.SessionId == sessionId).ToListAsync(ct);

        public async Task<SessionOffer?> GetBySessionAndOfferAsync(Guid sessionId, Guid offerId, CancellationToken ct = default)
            => await _context.SessionOffers.FirstOrDefaultAsync(so => so.SessionId == sessionId && so.OfferId == offerId, ct);
    }
}
