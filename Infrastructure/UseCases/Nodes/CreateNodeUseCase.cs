using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Nodes.Requests;
using Infrastructure.Contracts.Nodes.Responses;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Nodes;

/// <summary>
/// Use case: create a new node in a flow.
/// Validates the flow exists and parses the NodeType enum.
/// </summary>
public sealed class CreateNodeUseCase
{
    private readonly IFlowRepository _flows;
    private readonly INodeRepository _nodes;
    private readonly IUnitOfWork _uow;

    public CreateNodeUseCase(IFlowRepository flows, INodeRepository nodes, IUnitOfWork uow)
    {
        _flows = flows;
        _nodes = nodes;
        _uow = uow;
    }

    public async Task<FlowResult<NodeResponse>> ExecuteAsync(
        Guid flowId,
        CreateNodeRequest request,
        CancellationToken ct = default)
    {
        // Validate flow exists
        var flow = await _flows.GetByIdAsync(flowId, ct);
        if (flow == null)
            return FlowResult<NodeResponse>.NotFound("Flow not found.");

        // Parse NodeType enum
        if (!Enum.TryParse<NodeType>(request.Type, ignoreCase: true, out var nodeType))
            return FlowResult<NodeResponse>.Fail(
                $"Invalid node type '{request.Type}'. Must be one of: {string.Join(", ", Enum.GetNames(typeof(NodeType)))}",
                statusCode: 400);

        // Create node domain entity
        try
        {
            var node = Node.Create(
                flowId,
                nodeType,
                request.Title,
                request.AttributeKey ?? "",
                request.PositionX,
                request.PositionY);

            // Apply optional fields
            if (!string.IsNullOrWhiteSpace(request.Description))
                node.SetDescription(request.Description);

            if (!string.IsNullOrWhiteSpace(request.MediaUrl))
                node.SetMedia(request.MediaUrl);

            await _nodes.AddAsync(node, ct);
            await _uow.SaveChangesAsync();

            return FlowResult<NodeResponse>.Ok(ToResponse(node));
        }
        catch (ArgumentException ex)
        {
            return FlowResult<NodeResponse>.Fail(ex.Message, statusCode: 400);
        }
        catch (InvalidOperationException ex)
        {
            return FlowResult<NodeResponse>.Fail(ex.Message, statusCode: 422);
        }
    }

    private static NodeResponse ToResponse(Node node) =>
        new(
            node.Id,
            node.FlowId,
            node.Type.ToString(),
            node.AttributeKey,
            node.Title,
            node.Description,
            node.MediaUrl,
            node.PositionX,
            node.PositionY,
            node.CreatedAt);
}
