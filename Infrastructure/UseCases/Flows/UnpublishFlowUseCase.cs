using Application.Repositories.Interfaces;
using Domain.Services;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Flows;

/// <summary>
/// Use case: unpublish a flow, hiding it from end-users.
/// Does not delete data — the flow can be re-published later.
/// </summary>
public sealed class UnpublishFlowUseCase(
    IFlowRepository _flows,
    IUserProfileRepository _userProfiles,
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

        if (!flow.IsPublished)
            return FlowResult<FlowSummaryResponse>.Fail("Flow is not currently published.", statusCode: 409);

        flow.Unpublish(_time);

        _flows.Update(flow);
        await _uow.SaveChangesAsync();

        return FlowResult<FlowSummaryResponse>.Ok(FlowMapper.ToSummary(flow));
    }
}
