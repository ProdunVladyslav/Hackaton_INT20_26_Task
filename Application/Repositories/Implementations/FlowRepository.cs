using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Microsoft.EntityFrameworkCore;

namespace Application.Repositories.Implementations
{
    public class FlowRepository(AppDbContext context) : GenericRepository<Flow>(context), IFlowRepository
    {
        /// <inheritdoc/>
        public async Task<Flow?> GetFlowWithDagAsync(Guid flowId, CancellationToken ct = default)
        {
            // Single query using split-query strategy to avoid cartesian explosion when
            // a flow has many nodes each with many options.
            return await _context.Flows
                .Include(f => f.Nodes)
                    .ThenInclude(n => n.Options)
                .Include(f => f.Edges)
                // NodeOffers are not owned by the Node aggregate in Domain,
                // so we load them separately via the join table.
                .AsSplitQuery()
                .FirstOrDefaultAsync(f => f.Id == flowId, ct);
        }

        /// <inheritdoc/>
        public async Task<List<Flow>> GetAllOrderedAsync(CancellationToken ct = default)
        {
            return await _context.Flows
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync(ct);
        }

        /// <inheritdoc/>
        public async Task<Flow?> GetFirstPublishedWithDagAsync(CancellationToken ct = default)
        {
            return await _context.Flows
                .Include(f => f.Nodes).ThenInclude(n => n.Options)
                .Include(f => f.Edges)
                .AsSplitQuery()
                .Where(f => f.IsPublished)
                .OrderByDescending(f => f.UpdatedAt)
                .FirstOrDefaultAsync(ct);
        }

        /// <inheritdoc/>
        public async Task<Flow?> GetPublishedFlowWithDagAsync(Guid flowId, CancellationToken ct = default)
        {
            return await _context.Flows
                .Include(f => f.Nodes).ThenInclude(n => n.Options)
                .Include(f => f.Edges)
                .AsSplitQuery()
                .FirstOrDefaultAsync(f => f.Id == flowId && f.IsPublished, ct);
        }
    }
}
