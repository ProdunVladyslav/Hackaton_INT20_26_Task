using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Flows;

/// <summary>
/// Use case: permanently delete a flow and all its owned data
/// (nodes, edges, options cascade-deleted by DB foreign keys).
/// </summary>
public sealed class DeleteFlowUseCase
{
    private readonly IFlowRepository _flows;
    private readonly IUnitOfWork     _uow;

    public DeleteFlowUseCase(IFlowRepository flows, IUnitOfWork uow)
    {
        _flows = flows;
        _uow   = uow;
    }

    public async Task<FlowResult<bool>> ExecuteAsync(
        Guid flowId,
        CancellationToken ct = default)
    {
        var flow = await _flows.GetByIdAsync(flowId, ct);

        if (flow is null)
            return FlowResult<bool>.NotFound($"Flow {flowId} not found.");

        _flows.Remove(flow);
        await _uow.SaveChangesAsync();

        return FlowResult<bool>.Ok(true);
    }
}
