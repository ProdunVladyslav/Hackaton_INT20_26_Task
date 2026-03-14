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
    }
}
