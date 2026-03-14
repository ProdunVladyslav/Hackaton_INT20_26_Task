using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Flows;

/// <summary>
/// Use case: publish a flow so it becomes visible to end-users.
/// A flow can only be published when it has a designated entry node.
/// Domain model enforces this invariant.
/// </summary>
public sealed class PublishFlowUseCase
{
    private readonly IFlowRepository _flows;
    private readonly IUnitOfWork     _uow;

    public PublishFlowUseCase(IFlowRepository flows, IUnitOfWork uow)
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

        if (flow.IsPublished)
            return FlowResult<FlowSummaryResponse>.Fail("Flow is already published.", statusCode: 409);

        try
        {
            // Domain enforces: cannot publish without an entry node.
            flow.Publish();
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
