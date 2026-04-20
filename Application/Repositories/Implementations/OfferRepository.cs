using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Microsoft.EntityFrameworkCore;

namespace Application.Repositories.Implementations
{
    public class OfferRepository(AppDbContext context) : GenericRepository<Offer>(context), IOfferRepository
    {
        public async Task<List<Offer>> GetAllOrderedAsync(CancellationToken ct = default)
            => await _context.Offers.OrderBy(o => o.Name).ToListAsync(ct);

        public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken ct = default)
            => await _context.Offers.AnyAsync(o => o.Slug == slug && (excludeId == null || o.Id != excludeId), ct);

        public async Task<List<Offer>> GetAllOrderedByOwnerAsync(Guid ownerProfileId, CancellationToken ct = default)
            => await _context.Offers
                .Where(o => o.OwnerId == ownerProfileId)
                .OrderBy(o => o.Name)
                .ToListAsync(ct);
    }
}
