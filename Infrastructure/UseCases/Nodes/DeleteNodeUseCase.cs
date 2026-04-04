using Application;
using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Services.Validators;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.UseCases.Nodes;

/// <summary>
/// Use case: delete a node from a flow.
/// If the node is the entry point of the flow, the flow is unpublished.
/// </summary>
public sealed class DeleteNodeUseCase
{
    private readonly IFlowRepository _flows;
    private readonly INodeRepository _nodes;
    private readonly IUnitOfWork _uow;
    private readonly AppDbContext _db;

    public DeleteNodeUseCase(IFlowRepository flows, INodeRepository nodes, IUnitOfWork uow, AppDbContext db)
    {
        _flows = flows;
        _nodes = nodes;
        _uow = uow;
        _db = db;
    }

    public async Task<FlowResult<bool>> ExecuteAsync(
        Guid flowId,
        Guid nodeId,
        CancellationToken ct = default)
    {
        // ── 1. Load node ──────────────────────────────────────────────────────
        var node = await _nodes.GetByIdAsync(nodeId, ct);
        if (node == null)
            return FlowResult<bool>.NotFound("Node not found.");

        if (node.FlowId != flowId)
            return FlowResult<bool>.NotFound("Node not found in this flow.");

        // ── 2. Unpublish flow if node is entry point ───────────────────────────
        var flow = await _flows.GetByIdAsync(flowId, ct);
        if (flow != null && flow.EntryNodeId == nodeId)
        {
            try { flow.Unpublish(); }
            catch { /* already unpublished */ }
        }

        // ── 3. Delete NodeOffers (join table) ─────────────────────────────────
        var nodeOffers = await _db.NodeOffers
            .Where(no => no.NodeId == nodeId)
            .ToListAsync(ct);

        _db.NodeOffers.RemoveRange(nodeOffers);

        var nodesWithAttributeKey = await _db.Nodes
            .Where(n => n.AttributeKey == node.AttributeKey && n.Id != node.Id)
            .ToListAsync(ct);

        if (nodesWithAttributeKey.Count == 0)
        {
            var edgesWithAttributeKey = await _db.Edges
                .Where(e => e.ConditionsJson.Contains(node.AttributeKey))
                .ToListAsync(ct);

            foreach (var edge in edgesWithAttributeKey)
            {
                var cleaned = ConditionsJsonValidator.RemoveConditionsByKey(edge.ConditionsJson, node.AttributeKey);
                edge.UpdateConditions(cleaned);
            }
        }

        // ── 4. Delete all Edges where this node is source OR target ───────────
        var edges = await _db.Edges
            .Where(e => e.SourceNodeId == nodeId || e.TargetNodeId == nodeId)
            .ToListAsync(ct);

        _db.Edges.RemoveRange(edges);

        // ── 5. Delete the node itself ─────────────────────────────────────────
        _nodes.Remove(node);

        await _uow.SaveChangesAsync(ct);

        return FlowResult<bool>.Ok(true);
    }
}
