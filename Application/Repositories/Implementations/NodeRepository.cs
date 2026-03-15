using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Microsoft.EntityFrameworkCore;

namespace Application.Repositories.Implementations
{
    public class NodeRepository(AppDbContext context) : GenericRepository<Node>(context), INodeRepository
    {
        public override async Task<Node?> GetByIdAsync(object id, CancellationToken ct = default)
        {
            return await _dbSet
                .Include(n => n.Options)
                .FirstOrDefaultAsync(n => n.Id == (Guid)id, ct);
        }
    }
}
