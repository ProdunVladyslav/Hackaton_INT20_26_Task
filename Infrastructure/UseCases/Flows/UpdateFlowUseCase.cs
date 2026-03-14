using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Flows.Requests;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Flows;

/// <summary>
/// Use case: update the name and/or description of an existing flow.
/// Partial update — only fields present in the request are applied.
/// </summary>
public sealed class UpdateFlowUseCase
{
    private readonly IFlowRepository _flows;
    private readonly IUnitOfWork     _uow;

    public UpdateFlowUseCase(IFlowRepository flows, IUnitOfWork uow)
    {
        _flows = flows;
        _uow   = uow;
    }

    public async Task<FlowResult<FlowSummaryResponse>> ExecuteAsync(
        Guid              flowId,
        UpdateFlowRequest request,
        CancellationToken ct = default)
    {
        var flow = await _flows.GetByIdAsync(flowId, ct);

        if (flow is null)
            return FlowResult<FlowSummaryResponse>.NotFound($"Flow {flowId} not found.");

        // Apply only the fields provided in the request (partial update)
        if (request.Name is not null)
            flow.SetName(request.Name);

        if (request.Description is not null)
            flow.SetDescription(request.Description);

        _flows.Update(flow);
        await _uow.SaveChangesAsync();

        return FlowResult<FlowSummaryResponse>.Ok(FlowMapper.ToSummary(flow));
    }
}
