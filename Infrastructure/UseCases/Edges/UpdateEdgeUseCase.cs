using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Edges.Requests;
using Infrastructure.Contracts.Edges.Responses;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Edges;

/// <summary>
/// Use case: update an edge's properties (priority, conditions).
/// Only provided fields are updated.
/// </summary>
public sealed class UpdateEdgeUseCase
{
    private readonly IEdgeRepository _edges;
    private readonly IUnitOfWork _uow;

    public UpdateEdgeUseCase(IEdgeRepository edges, IUnitOfWork uow)
    {
        _edges = edges;
        _uow = uow;
    }

    public async Task<FlowResult<EdgeResponse>> ExecuteAsync(
        Guid flowId,
        Guid edgeId,
        UpdateEdgeRequest request,
        CancellationToken ct = default)
    {
        // Load edge
        var edge = await _edges.GetByIdAsync(edgeId, ct);
        if (edge == null)
            return FlowResult<EdgeResponse>.NotFound("Edge not found.");

        // Verify edge belongs to the flow
        if (edge.FlowId != flowId)
            return FlowResult<EdgeResponse>.NotFound("Edge not found in this flow.");

        // Apply updates
        try
        {
            if (request.Priority.HasValue)
                edge.SetPriority(request.Priority.Value);

            if (request.ConditionsJson != null)
                edge.UpdateConditions(request.ConditionsJson);

            _edges.Update(edge);
            await _uow.SaveChangesAsync();

            return FlowResult<EdgeResponse>.Ok(ToResponse(edge));
        }
        catch (ArgumentException ex)
        {
            return FlowResult<EdgeResponse>.Fail(ex.Message, statusCode: 400);
        }
    }

    private static EdgeResponse ToResponse(Edge edge)
    {
        return new EdgeResponse(
            edge.Id,
            edge.FlowId,
            edge.SourceNodeId,
            edge.TargetNodeId,
            edge.Priority,
            edge.ConditionsJson,
            edge.CreatedAt
        );
    }
}
