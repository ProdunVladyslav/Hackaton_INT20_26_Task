using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Microsoft.EntityFrameworkCore;

namespace Application.Repositories.Implementations
{
    public class NodeRedirectLinkRepository(AppDbContext context)
        : GenericRepository<NodeRedirectLink>(context), INodeRedirectLinkRepository
    {
        public async Task<List<NodeRedirectLink>> GetLinksByNodeRedirectIdAsync(
            Guid nodeRedirectId,
            CancellationToken ct = default)
        {
            return await context.Set<NodeRedirectLink>()
                .Where(l => l.NodeRedirectId == nodeRedirectId)
                .OrderBy(l => l.DisplayOrder)
                .ToListAsync(ct);
        }
    }
}
