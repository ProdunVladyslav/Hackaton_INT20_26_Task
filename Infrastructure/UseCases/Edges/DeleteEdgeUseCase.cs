using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Edges;

/// <summary>
/// Use case: delete an edge from a flow.
/// </summary>
public sealed class DeleteEdgeUseCase
{
    private readonly IEdgeRepository _edges;
    private readonly IUnitOfWork _uow;

    public DeleteEdgeUseCase(IEdgeRepository edges, IUnitOfWork uow)
    {
        _edges = edges;
        _uow = uow;
    }

    public async Task<FlowResult<bool>> ExecuteAsync(
        Guid flowId,
        Guid edgeId,
        CancellationToken ct = default)
    {
        // Load edge
        var edge = await _edges.GetByIdAsync(edgeId, ct);
        if (edge == null)
            return FlowResult<bool>.NotFound("Edge not found.");

        // Verify edge belongs to the flow
        if (edge.FlowId != flowId)
            return FlowResult<bool>.NotFound("Edge not found in this flow.");

        // Delete edge
        _edges.Remove(edge);
        await _uow.SaveChangesAsync();

        return FlowResult<bool>.Ok(true);
    }
}
