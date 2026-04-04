using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Flows.Requests;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Flows;

/// <summary>
/// Use case: designate a specific node as the entry point of a flow.
/// The node must already belong to the flow — the domain model enforces this.
/// </summary>
public sealed class SetEntryNodeUseCase
{
    private readonly IFlowRepository _flows;
    private readonly INodeRepository _nodes;
    private readonly IUnitOfWork     _uow;

    public SetEntryNodeUseCase(IFlowRepository flows, IUnitOfWork uow, INodeRepository nodes)
    {
        _flows = flows;
        _nodes = nodes;
        _uow = uow;
    }

    public async Task<FlowResult<FlowSummaryResponse>> ExecuteAsync(
        Guid                 flowId,
        SetEntryNodeRequest  request,
        CancellationToken    ct = default)
    {
        // We need the nodes collection loaded to validate ownership in domain.
        var flow = await _flows.GetFlowWithDagAsync(flowId, ct);

        if (flow is null)
            return FlowResult<FlowSummaryResponse>.NotFound($"Flow {flowId} not found.");

        var node = await _nodes.FirstOrDefaultAsync(n => n.Id == request.EntryNodeId);
        if (node is null)
            return FlowResult<FlowSummaryResponse>.NotFound($"Node {request.EntryNodeId} not found.");

        if (node.Type == NodeType.Offer)
            return FlowResult<FlowSummaryResponse>.Fail("Offer nodes cannot be entry nodes.", statusCode: 422);

        try
        {
            // Domain enforces: the node must belong to this flow.
            flow.SetEntryNode(request.EntryNodeId);
        }
        catch (InvalidOperationException ex)
        {
            return FlowResult<FlowSummaryResponse>.Fail(ex.Message, statusCode: 422);
        }

        _flows.Update(flow);
        await _uow.SaveChangesAsync();

        return FlowResult<FlowSummaryResponse>.Ok(FlowMapper.ToSummary(flow));
    }
}
