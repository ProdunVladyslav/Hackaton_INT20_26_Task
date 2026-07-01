using Application.Repositories.Interfaces;
using Domain.Services;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Flows;

/// <summary>
/// Use case: publish a flow so it becomes visible to end-users.
/// A flow can only be published when it has a designated entry node.
/// Domain model enforces this invariant.
/// </summary>
public sealed class PublishFlowUseCase(
    IFlowRepository _flows,
    IUserProfileRepository _userProfiles,
    INodeRepository _nodeRepository,
    IUnitOfWork _uow,
    IDateTimeProvider _time)
{
    public async Task<FlowResult<FlowSummaryResponse>> ExecuteAsync(
        Guid flowId,
        Guid applicationUserId,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(p => p.ApplicationUserId == applicationUserId, ct);
        if (profile == null)
            return FlowResult<FlowSummaryResponse>.NotFound("User profile not found.");

        var flow = await _flows.FirstOrDefaultAsync(f => f.Id == flowId && f.OwnerId == profile.Id, ct);
        if (flow is null)
            return FlowResult<FlowSummaryResponse>.NotFound($"Flow {flowId} not found.");

        if (flow.IsPublished)
            return FlowResult<FlowSummaryResponse>.Fail("Flow is already published.", statusCode: 409);

        var node = await _nodeRepository.FirstOrDefaultAsync(x => x.Id == flow.EntryNodeId);

        if(node is null || node.FlowId != flow.Id)
            return FlowResult<FlowSummaryResponse>.Fail("Node not found or does not belong to the flow.", statusCode: 422);

        try
        {
            flow.Publish(_time);
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