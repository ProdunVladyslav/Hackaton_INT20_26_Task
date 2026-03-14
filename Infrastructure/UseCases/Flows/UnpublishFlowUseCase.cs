using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Flows;

/// <summary>
/// Use case: unpublish a flow, hiding it from end-users.
/// Does not delete data — the flow can be re-published later.
/// </summary>
public sealed class UnpublishFlowUseCase
{
    private readonly IFlowRepository _flows;
    private readonly IUnitOfWork     _uow;

    public UnpublishFlowUseCase(IFlowRepository flows, IUnitOfWork uow)
    {
        _flows = flows;
        _uow   = uow;
    }

    public async Task<FlowResult<FlowSummaryResponse>> ExecuteAsync(
        Guid flowId,
        CancellationToken ct = default)
    {
        var flow = await _flows.GetByIdAsync(flowId, ct);

        if (flow is null)
            return FlowResult<FlowSummaryResponse>.NotFound($"Flow {flowId} not found.");

        if (!flow.IsPublished)
            return FlowResult<FlowSummaryResponse>.Fail("Flow is not currently published.", statusCode: 409);

        flow.Unpublish();

        _flows.Update(flow);
        await _uow.SaveChangesAsync();

        return FlowResult<FlowSummaryResponse>.Ok(FlowMapper.ToSummary(flow));
    }
}
