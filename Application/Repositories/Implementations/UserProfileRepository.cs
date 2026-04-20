using Application.Contracts.Auth;
using Application.Repositories.Interfaces;
using Domain.Model.AdminProfile;
using Microsoft.EntityFrameworkCore;

namespace Application.Repositories.Implementations
{
    public class UserProfileRepository(AppDbContext context) : GenericRepository<UserProfile>(context), IUserProfileRepository
    {
        public async Task<MeResponse?> GetMeAsync(Guid applicationUserId, CancellationToken ct = default)
            => await _context.Users
                .Include(u => u.Profile)
                .Where(u => u.Id == applicationUserId)
                .Select(u => new MeResponse(
                    u.Id,
                    u.Email!,
                    u.UserName!,
                    u.Profile != null ? u.Profile.Id : null
                ))
                .FirstOrDefaultAsync(ct);
    }
}
