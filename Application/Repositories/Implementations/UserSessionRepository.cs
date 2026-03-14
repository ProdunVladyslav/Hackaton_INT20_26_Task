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
    public class UserSessionRepository(AppDbContext context) : GenericRepository<UserSession>(context), IUserSessionRepository
    {
        public async Task<UserSession?> GetWithAnswersAsync(Guid sessionId, CancellationToken ct = default)
            => await _context.UserSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);
    }
}
