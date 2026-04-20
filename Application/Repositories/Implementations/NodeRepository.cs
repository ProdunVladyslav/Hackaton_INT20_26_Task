using Application.Contracts.Analytics;
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
                .Include(n => n.LeadCapture)
                    .ThenInclude(lc => lc.Fields)
                .Include(n => n.Redirect)
                    .ThenInclude(r => r.Links)
                .FirstOrDefaultAsync(n => n.Id == (Guid)id, ct);
        }

        public async Task<Dictionary<Guid, NodeCountStats>> GetNodeCountsByFlowsAsync(CancellationToken ct = default)
            => (await _context.Nodes
                .GroupBy(n => n.FlowId)
                .Select(g => new
                {
                    FlowId = g.Key,
                    NodeCount = g.Count(),
                    QuestionCount = g.Count(n => n.Type == NodeType.Question),
                    OfferCount = g.Count(n => n.Type == NodeType.Offer),
                    InfoPageCount = g.Count(n => n.Type == NodeType.InfoPage),
                })
                .ToListAsync(ct))
                .ToDictionary(x => x.FlowId, x => new NodeCountStats(
                    x.NodeCount, x.QuestionCount, x.OfferCount, x.InfoPageCount));

        public async Task<List<Node>> GetByAttributeKeyAsync(Guid flowId, string attributeKey, Guid excludeNodeId, CancellationToken ct = default)
            => await _context.Nodes
                .Where(n => n.FlowId == flowId && n.AttributeKey == attributeKey && n.Id != excludeNodeId)
                .ToListAsync(ct);

        public async Task<Node?> GetWithOptionsAsync(Guid nodeId, CancellationToken ct = default)
            => await _context.Nodes
                .Include(n => n.Options)
                .Include(n => n.LeadCapture)
                    .ThenInclude(lc => lc.Fields)
                .Include(n => n.Redirect)
                    .ThenInclude(r => r.Links)
                .FirstOrDefaultAsync(n => n.Id == nodeId, ct);

        public async Task<List<Node>> GetByFlowAsync(Guid flowId, CancellationToken ct = default)
            => await _context.Nodes
                .Where(n => n.FlowId == flowId)
                .ToListAsync(ct);
    }
}
