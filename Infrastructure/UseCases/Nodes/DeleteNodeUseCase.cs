using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Flows.Responses;

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

    public DeleteNodeUseCase(IFlowRepository flows, INodeRepository nodes, IUnitOfWork uow)
    {
        _flows = flows;
        _nodes = nodes;
        _uow = uow;
    }

    public async Task<FlowResult<bool>> ExecuteAsync(
        Guid flowId,
        Guid nodeId,
        CancellationToken ct = default)
    {
        // Load node
        var node = await _nodes.GetByIdAsync(nodeId, ct);
        if (node == null)
            return FlowResult<bool>.NotFound("Node not found.");

        // Verify node belongs to the flow
        if (node.FlowId != flowId)
            return FlowResult<bool>.NotFound("Node not found in this flow.");

        // If node is entry point, unpublish the flow
        var flow = await _flows.GetByIdAsync(flowId, ct);
        if (flow != null && flow.EntryNodeId == nodeId)
        {
            try
            {
                flow.Unpublish();
            }
            catch
            {
                // Flow might already be unpublished, ignore
            }
        }

        // Delete node
        _nodes.Remove(node);
        await _uow.SaveChangesAsync();

        return FlowResult<bool>.Ok(true);
    }
}
