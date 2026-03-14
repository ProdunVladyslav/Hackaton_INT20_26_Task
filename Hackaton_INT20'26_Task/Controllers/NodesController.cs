using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.Nodes.Requests;
using Infrastructure.Contracts.Nodes.Responses;
using Infrastructure.UseCases.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Hackaton_INT20_26_Task.Controllers;

/// <summary>
/// Admin endpoints for managing nodes within flows.
/// All endpoints require an authenticated session (cookie).
/// </summary>
[ApiController]
[Route("api/admin/flows/{flowId:guid}/nodes")]
[Authorize]
[Produces("application/json")]
[Tags("Admin — Nodes")]
public sealed class NodesController : ControllerBase
{
    private readonly CreateNodeUseCase _createNode;
    private readonly UpdateNodeUseCase _updateNode;
    private readonly MoveNodeUseCase _moveNode;
    private readonly DeleteNodeUseCase _deleteNode;

    public NodesController(
        CreateNodeUseCase createNode,
        UpdateNodeUseCase updateNode,
        MoveNodeUseCase moveNode,
        DeleteNodeUseCase deleteNode)
    {
        _createNode = createNode;
        _updateNode = updateNode;
        _moveNode = moveNode;
        _deleteNode = deleteNode;
    }

    // ── POST /api/admin/flows/{flowId}/nodes ──────────────────────────────

    [HttpPost]
    [SwaggerOperation(
        Summary = "Create node",
        Description = "Creates a new node in the flow. Type must be one of: Question, InfoPage, Offer.",
        OperationId = "AdminNodes_Create")]
    [SwaggerResponse(201, "Node created.", typeof(NodeResponse))]
    [SwaggerResponse(400, "Validation error (invalid type or missing title).")]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Flow not found.")]
    [SwaggerResponse(422, "Unprocessable entity (e.g., Question node without attribute key).")]
    public async Task<IActionResult> CreateNode(
        [FromRoute] Guid flowId,
        [FromBody] CreateNodeRequest request,
        CancellationToken ct)
    {
        var result = await _createNode.ExecuteAsync(flowId, request, ct);

        if (!result.Success)
            return ToActionResult(result);

        return StatusCode(201, result.Data);
    }

    // ── PUT /api/admin/flows/{flowId}/nodes/{nodeId} ──────────────────────

    [HttpPut("{nodeId:guid}")]
    [SwaggerOperation(
        Summary = "Update node",
        Description = "Updates a node's title, attributeKey, description, and/or mediaUrl. Only provided fields are changed.",
        OperationId = "AdminNodes_Update")]
    [SwaggerResponse(200, "Updated node.", typeof(NodeResponse))]
    [SwaggerResponse(400, "Validation error.")]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Node or flow not found.")]
    [SwaggerResponse(422, "Unprocessable entity.")]
    public async Task<IActionResult> UpdateNode(
        [FromRoute] Guid flowId,
        [FromRoute] Guid nodeId,
        [FromBody] UpdateNodeRequest request,
        CancellationToken ct)
    {
        var result = await _updateNode.ExecuteAsync(flowId, nodeId, request, ct);
        return ToActionResult(result);
    }

    // ── PUT /api/admin/flows/{flowId}/nodes/{nodeId}/position ─────────────

    [HttpPut("{nodeId:guid}/position")]
    [SwaggerOperation(
        Summary = "Move node",
        Description = "Updates the x,y position of a node on the canvas.",
        OperationId = "AdminNodes_Move")]
    [SwaggerResponse(200, "Updated node position.", typeof(NodeResponse))]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Node or flow not found.")]
    public async Task<IActionResult> MoveNode(
        [FromRoute] Guid flowId,
        [FromRoute] Guid nodeId,
        [FromBody] MoveNodeRequest request,
        CancellationToken ct)
    {
        var result = await _moveNode.ExecuteAsync(flowId, nodeId, request, ct);
        return ToActionResult(result);
    }

    // ── DELETE /api/admin/flows/{flowId}/nodes/{nodeId} ───────────────────

    [HttpDelete("{nodeId:guid}")]
    [SwaggerOperation(
        Summary = "Delete node",
        Description = "Permanently deletes a node and all its options. If the node is the flow's entry point, the flow is unpublished.",
        OperationId = "AdminNodes_Delete")]
    [SwaggerResponse(204, "Node deleted.")]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Node or flow not found.")]
    public async Task<IActionResult> DeleteNode(
        [FromRoute] Guid flowId,
        [FromRoute] Guid nodeId,
        CancellationToken ct)
    {
        var result = await _deleteNode.ExecuteAsync(flowId, nodeId, ct);

        if (!result.Success)
            return ToActionResult(result);

        return NoContent();
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private IActionResult ToActionResult<T>(FlowResult<T> result)
    {
        if (result.Success)
            return Ok(result.Data);

        return result.StatusCode switch
        {
            404 => NotFound(new { message = result.ErrorMessage }),
            409 => Conflict(new { message = result.ErrorMessage }),
            422 => UnprocessableEntity(new { message = result.ErrorMessage }),
            _   => BadRequest(new { message = result.ErrorMessage })
        };
    }
}
