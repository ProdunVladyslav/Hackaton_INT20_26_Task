using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Microsoft.EntityFrameworkCore;

namespace Application.Repositories.Implementations
{
    public class EdgeRepository(AppDbContext context) : GenericRepository<Edge>(context), IEdgeRepository
    {
        public async Task<Edge?> GetByIdWithOwnerCheckAsync(Guid edgeId, Guid userProfileId, CancellationToken ct = default)
            => await _context.Edges
                .Join(_context.Flows, e => e.FlowId, f => f.Id, (e, f) => new { e, f })
                .Where(x => x.e.Id == edgeId && x.f.OwnerId == userProfileId)
                .Select(x => x.e)
                .FirstOrDefaultAsync(ct);

        public async Task<Dictionary<Guid, int>> GetEdgeCountsByFlowsAsync(CancellationToken ct = default)
            => (await _context.Edges
                .GroupBy(e => e.FlowId)
                .Select(g => new { FlowId = g.Key, Count = g.Count() })
                .ToListAsync(ct))
                .ToDictionary(x => x.FlowId, x => x.Count);

        public async Task<List<Edge>> GetByNodeIdAsync(Guid nodeId, CancellationToken ct = default)
            => await _context.Edges
                .Where(e => e.SourceNodeId == nodeId || e.TargetNodeId == nodeId)
                .ToListAsync(ct);

        public async Task<List<Edge>> GetByAttributeKeyAsync(Guid flowId, string attributeKey, CancellationToken ct = default)
            => await _context.Edges
                .Where(e => e.FlowId == flowId && e.ConditionsJson.Contains(attributeKey))
                .ToListAsync(ct);

        public async Task<List<Edge>> GetBySourceNodeAsync(Guid sourceNodeId, Guid flowId, CancellationToken ct = default)
            => await _context.Edges
                .Where(e => e.SourceNodeId == sourceNodeId && e.FlowId == flowId)
                .OrderByDescending(e => e.Priority)
                .ToListAsync(ct);

        public async Task<bool> HasOutgoingEdgesAsync(Guid nodeId, Guid flowId, CancellationToken ct = default)
            => await _context.Edges
                .AnyAsync(e => e.SourceNodeId == nodeId && e.FlowId == flowId, ct);

        public async Task<List<Edge>> GetByFlowAsync(Guid flowId, CancellationToken ct = default)
            => await _context.Edges
                .Where(e => e.FlowId == flowId)
                .ToListAsync(ct);
    }
}
