using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Edges.Requests;
using Infrastructure.Contracts.Edges.Responses;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Services.Validators;

namespace Infrastructure.UseCases.Edges;

/// <summary>
/// Use case: create a new edge (connection) between two nodes in a flow.
/// Validates that both nodes exist and belong to the same flow.
/// Prevents duplicate edges between the same source and target.
/// </summary>
public sealed class CreateEdgeUseCase
{
    private readonly IFlowRepository _flows;
    private readonly INodeRepository _nodes;
    private readonly IEdgeRepository _edges;
    private readonly IUnitOfWork _uow;

    public CreateEdgeUseCase(IFlowRepository flows, INodeRepository nodes, IEdgeRepository edges, IUnitOfWork uow)
    {
        _flows = flows;
        _nodes = nodes;
        _edges = edges;
        _uow = uow;
    }

    public async Task<FlowResult<EdgeResponse>> ExecuteAsync(
        Guid flowId,
        CreateEdgeRequest request,
        CancellationToken ct = default)
    {
        // Validate flow exists
        var flow = await _flows.GetByIdAsync(flowId, ct);
        if (flow == null)
            return FlowResult<EdgeResponse>.NotFound("Flow not found.");

        // Validate source node exists and belongs to flow
        var sourceNodeExists = await _nodes.AnyAsync(n => n.Id == request.SourceNodeId && n.FlowId == flowId, ct);
        if (!sourceNodeExists)
            return FlowResult<EdgeResponse>.Fail(
                "Source node not found in this flow.",
                statusCode: 404);

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

        // Validate target node exists and belongs to flow
        var targetNodeExists = await _nodes.AnyAsync(n => n.Id == request.TargetNodeId && n.FlowId == flowId, ct);
        if (!targetNodeExists)
            return FlowResult<EdgeResponse>.Fail(
                "Target node not found in this flow.",
                statusCode: 404);

        // Check for duplicate edge
        var edgeExists = await _edges.AnyAsync(
            e => e.FlowId == flowId && e.SourceNodeId == request.SourceNodeId && e.TargetNodeId == request.TargetNodeId,
            ct);
        if (edgeExists)
            return FlowResult<EdgeResponse>.Fail(
                "An edge already exists between these nodes.",
                statusCode: 409);

        // Create edge
        try
        {
            var edge = Edge.Create(
                flowId,
                request.SourceNodeId,
                request.TargetNodeId,
                request.Priority,
                request.ConditionsJson ?? "");

            await _edges.AddAsync(edge, ct);
            await _uow.SaveChangesAsync();

            return FlowResult<EdgeResponse>.Ok(ToResponse(edge));
        }
        catch (ArgumentException ex)
        {
            return FlowResult<EdgeResponse>.Fail(ex.Message, statusCode: 400);
        }
    }

    private static EdgeResponse ToResponse(Edge edge) =>
        new(
            edge.Id,
            edge.FlowId,
            edge.SourceNodeId,
            edge.TargetNodeId,
            edge.Priority,
            edge.ConditionsJson,
            edge.CreatedAt);
}
