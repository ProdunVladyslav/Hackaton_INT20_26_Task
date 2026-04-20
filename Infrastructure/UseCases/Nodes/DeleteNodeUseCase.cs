using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Services.Validators;

namespace Infrastructure.UseCases.Nodes;

/// <summary>
/// Use case: delete a node from a flow.
/// If the node is the entry point of the flow, the flow is unpublished.
/// </summary>
public sealed class DeleteNodeUseCase(
    IFlowRepository _flows,
    INodeRepository _nodes,
    INodeOfferRepository _nodeOffers,
    IEdgeRepository _edges,
    IUserSessionRepository _userSessions,
    IUserProfileRepository _userProfiles,
    IUnitOfWork _uow)
{
    public async Task<FlowResult<bool>> ExecuteAsync(
        Guid flowId,
        Guid nodeId,
        Guid applicationUserId,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(p => p.ApplicationUserId == applicationUserId, ct);
        if (profile == null)
            return FlowResult<bool>.NotFound("User profile not found.");

        var flow = await _flows.FirstOrDefaultAsync(f => f.Id == flowId && f.OwnerId == profile.Id, ct);
        if (flow == null)
            return FlowResult<bool>.NotFound("Flow not found.");

        var node = await _nodes.GetByIdAsync(nodeId, ct);
        if (node == null)
            return FlowResult<bool>.NotFound("Node not found.");

        if (node.FlowId != flowId)
            return FlowResult<bool>.NotFound("Node not found in this flow.");

        // ── Unpublish flow if node is entry point ─────────────────────────────
        if (flow.EntryNodeId == nodeId)
        {
            try { flow.Unpublish(); } catch { /* already unpublished */ }
        }

        // ── Clean up conditions referencing this AttributeKey ─────────────────
        if (!string.IsNullOrWhiteSpace(node.AttributeKey))
        {
            var siblingsWithKey = await _nodes.GetByAttributeKeyAsync(flowId, node.AttributeKey, nodeId, ct);
            if (siblingsWithKey.Count == 0)
            {
                var affectedEdges = await _edges.GetByAttributeKeyAsync(flowId, node.AttributeKey, ct);
                foreach (var edge in affectedEdges)
                {
                    var cleaned = ConditionsJsonParser.RemoveConditionsByKey(edge.ConditionsJson, node.AttributeKey);
                    edge.UpdateConditions(cleaned);
                }
            }
        }

        var sessions = await _userSessions.GetByCurrentNodeIdAsync(nodeId, ct);

        foreach (var session in sessions)
            session.ClearCurrentNode();

        // ── Delete NodeOffers, Edges, Node ────────────────────────────────────
        await _nodeOffers.DeleteByNodeIdAsync(nodeId, ct);

        var edges = await _edges.GetByNodeIdAsync(nodeId, ct);
        _edges.RemoveRange(edges);

        _nodes.Remove(node);

        await _uow.SaveChangesAsync(ct);

        return FlowResult<bool>.Ok(true);
    }
}
