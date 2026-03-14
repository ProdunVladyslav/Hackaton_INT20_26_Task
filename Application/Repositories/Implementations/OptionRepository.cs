using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Microsoft.EntityFrameworkCore;

namespace Application.Repositories.Implementations
{
    public class OptionRepository(AppDbContext context) : GenericRepository<Option>(context), IOptionRepository
    {
        public Task<List<Option>> GetOptionsByNodeIdAsync(Guid nodeId, CancellationToken ct = default)
            => _dbSet.Where(o => o.NodeId == nodeId).OrderBy(o => o.DisplayOrder).ToListAsync(ct);
    }
}
