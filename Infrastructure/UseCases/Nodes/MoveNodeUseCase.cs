using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Nodes.Requests;
using Infrastructure.Contracts.Nodes.Responses;
using Infrastructure.Contracts.Flows.Responses;
using System.Security.Cryptography.X509Certificates;

namespace Infrastructure.UseCases.Nodes;

/// <summary>
/// Use case: move a node to new x,y coordinates.
/// </summary>
public sealed class MoveNodeUseCase(
    INodeRepository _nodes,
    IFlowRepository _flows,
    IUserProfileRepository _userProfiles,
    IUnitOfWork _uow)
{
    public async Task<FlowResult<NodeResponse>> ExecuteAsync(
        Guid flowId,
        Guid nodeId,
        Guid applicationUserId,
        MoveNodeRequest request,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(p => p.ApplicationUserId == applicationUserId, ct);
        if (profile == null)
            return FlowResult<NodeResponse>.NotFound("User profile not found.");

        var flow = await _flows.FirstOrDefaultAsync(f => f.Id == flowId && f.OwnerId == profile.Id, ct);
        if (flow == null)
            return FlowResult<NodeResponse>.NotFound("Flow not found.");

        var node = await _nodes.GetByIdAsync(nodeId, ct);
        if (node == null)
            return FlowResult<NodeResponse>.NotFound("Node not found.");

        if (node.FlowId != flowId)
            return FlowResult<NodeResponse>.NotFound("Node not found in this flow.");

        float previousXPos = node.PositionX;
        float previousYPos = node.PositionY;

        try
        {
            node.Move(request.PositionX, request.PositionY);

            var affected = await _uow.SaveChangesAsync(ct);

            var positionChanged = !(previousXPos == node.PositionX && previousYPos == node.PositionY);

            if (affected == 0 && positionChanged)
                return FlowResult<NodeResponse>.Fail("Save wrote 0 rows — position not persisted.", 500);

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
            node.CreatedAt,
            node.AnswerType?.ToString(),
            node.SliderMin,
            node.SliderMax);
}
