using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Nodes.Requests;
using Infrastructure.Contracts.Nodes.Responses;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Nodes;

/// <summary>
/// Use case: update a node's properties (title, attributeKey, description, media).
/// Only provided fields are updated.
/// </summary>
public sealed class UpdateNodeUseCase
{
    private readonly INodeRepository _nodes;
    private readonly IUnitOfWork _uow;

    public UpdateNodeUseCase(INodeRepository nodes, IUnitOfWork uow)
    {
        _nodes = nodes;
        _uow = uow;
    }

    public async Task<FlowResult<NodeResponse>> ExecuteAsync(
        Guid flowId,
        Guid nodeId,
        UpdateNodeRequest request,
        CancellationToken ct = default)
    {
        // Load node
        var node = await _nodes.GetByIdAsync(nodeId, ct);
        if (node == null)
            return FlowResult<NodeResponse>.NotFound("Node not found.");

        // Verify node belongs to the flow
        if (node.FlowId != flowId)
            return FlowResult<NodeResponse>.NotFound("Node not found in this flow.");

        // Apply updates
        try
        {
            if (!string.IsNullOrWhiteSpace(request.Title))
                node.SetTitle(request.Title);

            if (request.AttributeKey != null)
                node.SetAttributeKey(request.AttributeKey);

            if (request.Description != null)
                node.SetDescription(request.Description);

            if (request.MediaUrl != null)
                node.SetMedia(request.MediaUrl);

            _nodes.Update(node);
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
