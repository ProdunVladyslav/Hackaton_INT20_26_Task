using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Nodes.Requests;
using Infrastructure.Contracts.Nodes.Responses;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Nodes;

/// <summary>
/// Use case: move a node to new x,y coordinates.
/// </summary>
public sealed class MoveNodeUseCase
{
    private readonly INodeRepository _nodes;
    private readonly IUnitOfWork _uow;

    public MoveNodeUseCase(INodeRepository nodes, IUnitOfWork uow)
    {
        _nodes = nodes;
        _uow = uow;
    }

    public async Task<FlowResult<NodeResponse>> ExecuteAsync(
        Guid flowId,
        Guid nodeId,
        MoveNodeRequest request,
        CancellationToken ct = default)
    {
        // Load node
        var node = await _nodes.GetByIdAsync(nodeId, ct);
        if (node == null)
            return FlowResult<NodeResponse>.NotFound("Node not found.");

        // Verify node belongs to the flow
        if (node.FlowId != flowId)
            return FlowResult<NodeResponse>.NotFound("Node not found in this flow.");

        // Move node
        try
        {
            node.Move(request.PositionX, request.PositionY);

            _nodes.Update(node);
            await _uow.SaveChangesAsync();

            return FlowResult<NodeResponse>.Ok(ToResponse(node));
        }
        catch (ArgumentException ex)
        {
            return FlowResult<NodeResponse>.Fail(ex.Message, statusCode: 400);
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
