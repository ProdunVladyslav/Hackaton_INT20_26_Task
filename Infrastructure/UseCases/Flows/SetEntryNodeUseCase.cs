using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Domain.Services;
using Infrastructure.Contracts.Flows.Requests;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Flows;

/// <summary>
/// Use case: designate a specific node as the entry point of a flow.
/// The node must already belong to the flow — the domain model enforces this.
/// </summary>
public sealed class SetEntryNodeUseCase(
    IFlowRepository _flows,
    INodeRepository _nodes,
    IUserProfileRepository _userProfiles,
    IUnitOfWork _uow,
    IDateTimeProvider _time)
{
    public async Task<FlowResult<FlowSummaryResponse>> ExecuteAsync(
        Guid flowId,
        Guid applicationUserId,
        SetEntryNodeRequest request,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(p => p.ApplicationUserId == applicationUserId, ct);
        if (profile == null)
            return FlowResult<FlowSummaryResponse>.NotFound("User profile not found.");

        var flow = await _flows.GetFlowWithDagAsync(flowId, profile.Id, ct);
        if (flow is null)
            return FlowResult<FlowSummaryResponse>.NotFound($"Flow {flowId} not found.");

        var node = await _nodes.FirstOrDefaultAsync(n => n.Id == request.EntryNodeId, ct);
        if (node is null)
            return FlowResult<FlowSummaryResponse>.NotFound($"Node {request.EntryNodeId} not found.");

        if (node.Type == NodeType.Offer)
            return FlowResult<FlowSummaryResponse>.Fail("Offer nodes cannot be entry nodes.", statusCode: 422);

        try
        {
            flow.SetEntryNode(request.EntryNodeId, _time);
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
