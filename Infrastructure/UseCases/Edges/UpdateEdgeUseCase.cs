using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Edges.Requests;
using Infrastructure.Contracts.Edges.Responses;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Services.Validators;

namespace Infrastructure.UseCases.Edges;

/// <summary>
/// Use case: update an edge's properties (priority, conditions).
/// Only provided fields are updated.
/// </summary>
public sealed class UpdateEdgeUseCase
{
    private readonly IEdgeRepository _edges;
    private readonly INodeRepository _nodes;
    private readonly IUnitOfWork _uow;

    public UpdateEdgeUseCase(IEdgeRepository edges, INodeRepository nodes, IUnitOfWork uow)
    {
        _edges = edges;
        _nodes = nodes;
        _uow = uow;
    }

    public async Task<FlowResult<EdgeResponse>> ExecuteAsync(
        Guid flowId,
        Guid edgeId,
        UpdateEdgeRequest request,
        CancellationToken ct = default)
    {
        var edge = await _edges.GetByIdAsync(edgeId, ct);
        if (edge == null)
            return FlowResult<EdgeResponse>.NotFound("Edge not found.");

        if (edge.FlowId != flowId)
            return FlowResult<EdgeResponse>.NotFound("Edge not found in this flow.");

        // ── Validate ConditionsJson if provided ───────────────────────────────
        if (request.ConditionsJson != null)
        {
            var formatError = ConditionsJsonValidator.Validate(request.ConditionsJson);
            if (formatError is not null)
                return FlowResult<EdgeResponse>.Fail(formatError, statusCode: 400);

            var referencedKeys = ConditionsJsonValidator.GetAttributeKeys(request.ConditionsJson);
            foreach (var keyValue in referencedKeys)
            {
                var nodeWithKey = await _nodes.FirstOrDefaultAsync(
                    n => n.FlowId == flowId && n.AttributeKey == keyValue.Key, ct);

                if (nodeWithKey == null)
                    return FlowResult<EdgeResponse>.Fail(
                        $"AttributeKey '{keyValue.Key}' is not defined by any node in this flow.",
                        statusCode: 422);

                var usedOperators = keyValue.Value
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                var allowedOperators = nodeWithKey.ValueKind switch
                {
                    ValueKind.Text => new[] { "eq", "neq", "in" },
                    ValueKind.Numeric => new[] { "eq", "neq", "in", "gt", "gte", "lt", "lte", "between" },
                    _ => new[] { "eq", "neq" }
                };

                var invalidOps = usedOperators.Except(allowedOperators, StringComparer.OrdinalIgnoreCase).ToList();
                if (invalidOps.Count > 0)
                    return FlowResult<EdgeResponse>.Fail(
                        $"AttributeKey '{keyValue.Key}' has ValueKind '{nodeWithKey.ValueKind}' which does not support " +
                        $"operator(s): {string.Join(", ", invalidOps)}. " +
                        $"Allowed: {string.Join(", ", allowedOperators)}.",
                        statusCode: 422);
            }
        }

        // ── Apply updates ─────────────────────────────────────────────────────
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

    private static EdgeResponse ToResponse(Edge edge) =>
        new(edge.Id, edge.FlowId, edge.SourceNodeId, edge.TargetNodeId,
            edge.Priority, edge.ConditionsJson, edge.CreatedAt);
}
