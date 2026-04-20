using Infrastructure.Contracts.Edges.Requests;
using Infrastructure.Contracts.Edges.Responses;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.UseCases.Edges;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace Hackaton_INT20_26_Task.Controllers;

/// <summary>
/// Admin endpoints for managing edges (connections) within flows.
/// All endpoints require an authenticated session (cookie).
/// </summary>
[ApiController]
[Route("api/admin/flows/{flowId:guid}/edges")]
[Authorize]
[Produces("application/json")]
[Tags("Admin — Edges")]
public sealed class EdgesController(
    CreateEdgeUseCase _createEdge,
    UpdateEdgeUseCase _updateEdge,
    DeleteEdgeUseCase _deleteEdge) : ControllerBase
{
    // ── POST /api/admin/flows/{flowId}/edges ──────────────────────────────

    [HttpPost]
    [SwaggerOperation(
        Summary = "Create edge",
        Description = "Creates a new edge (connection) between two nodes in the flow. Both nodes must exist in the flow.",
        OperationId = "AdminEdges_Create")]
    [SwaggerResponse(201, "Edge created.", typeof(EdgeResponse))]
    [SwaggerResponse(400, "Validation error (e.g., source equals target).")]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Flow or one of the nodes not found.")]
    [SwaggerResponse(409, "An edge already exists between these nodes.")]
    public async Task<IActionResult> CreateEdge(
        [FromRoute] Guid flowId,
        [FromBody] CreateEdgeRequest request,
        CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var result = await _createEdge.ExecuteAsync(flowId, Guid.Parse(userId), request, ct);

        if (!result.Success)
            return ToActionResult(result);

        return StatusCode(201, result.Data);
    }

    // ── PUT /api/admin/flows/{flowId}/edges/{edgeId} ──────────────────────

    [HttpPut("{edgeId:guid}")]
    [SwaggerOperation(
        Summary = "Update edge",
        Description = "Updates an edge's priority and/or conditions. Only provided fields are changed.",
        OperationId = "AdminEdges_Update")]
    [SwaggerResponse(200, "Updated edge.", typeof(EdgeResponse))]
    [SwaggerResponse(400, "Validation error.")]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Edge or flow not found.")]
    public async Task<IActionResult> UpdateEdge(
        [FromRoute] Guid flowId,
        [FromRoute] Guid edgeId,
        [FromBody] UpdateEdgeRequest request,
        CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var result = await _updateEdge.ExecuteAsync(flowId, edgeId, Guid.Parse(userId), request, ct);
        return ToActionResult(result);
    }

    // ── DELETE /api/admin/flows/{flowId}/edges/{edgeId} ───────────────────

    [HttpDelete("{edgeId:guid}")]
    [SwaggerOperation(
        Summary = "Delete edge",
        Description = "Permanently deletes an edge.",
        OperationId = "AdminEdges_Delete")]
    [SwaggerResponse(204, "Edge deleted.")]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Edge or flow not found.")]
    public async Task<IActionResult> DeleteEdge(
        [FromRoute] Guid flowId,
        [FromRoute] Guid edgeId,
        CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var result = await _deleteEdge.ExecuteAsync(flowId, Guid.Parse(userId), edgeId, ct);

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
