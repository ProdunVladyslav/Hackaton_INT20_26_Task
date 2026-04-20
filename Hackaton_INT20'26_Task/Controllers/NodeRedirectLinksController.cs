using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.NodeRedirectLinks.Requests;
using Infrastructure.Contracts.NodeRedirectLinks.Responses;
using Infrastructure.UseCases.NodeRedirectLinks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace Hackaton_INT20_26_Task.Controllers;

/// <summary>
/// Admin endpoints for managing redirect links within Redirect nodes.
/// All endpoints require an authenticated session (cookie).
/// </summary>
[ApiController]
[Route("api/admin/nodes/{nodeId:guid}/redirect-links")]
[Authorize]
[Produces("application/json")]
[Tags("Admin — Redirect Links")]
public sealed class NodeRedirectLinksController : ControllerBase
{
    private readonly CreateNodeRedirectLinkUseCase _createLink;
    private readonly UpdateNodeRedirectLinkUseCase _updateLink;
    private readonly DeleteNodeRedirectLinkUseCase _deleteLink;
    private readonly ReorderNodeRedirectLinksUseCase _reorderLinks;

    public NodeRedirectLinksController(
        CreateNodeRedirectLinkUseCase createLink,
        UpdateNodeRedirectLinkUseCase updateLink,
        DeleteNodeRedirectLinkUseCase deleteLink,
        ReorderNodeRedirectLinksUseCase reorderLinks)
    {
        _createLink = createLink;
        _updateLink = updateLink;
        _deleteLink = deleteLink;
        _reorderLinks = reorderLinks;
    }

    private bool TryGetUserId(out Guid userId)
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    // ── POST /api/admin/nodes/{nodeId}/redirect-links ─────────────────────

    [HttpPost]
    [SwaggerOperation(Summary = "Create redirect link", OperationId = "AdminRedirectLinks_Create")]
    [SwaggerResponse(201, "Redirect link created.", typeof(NodeRedirectLinkResponse))]
    [SwaggerResponse(400, "Validation error.")]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Node not found.")]
    [SwaggerResponse(422, "Node is not a Redirect type, or maximum 3 links reached.")]
    public async Task<IActionResult> CreateLink(
        [FromRoute] Guid nodeId,
        [FromBody] CreateNodeRedirectLinkRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _createLink.ExecuteAsync(nodeId, userId, request, ct);

        if (!result.Success)
            return ToActionResult(result);

        return StatusCode(201, result.Data);
    }

    // ── PUT /api/admin/nodes/{nodeId}/redirect-links/{linkId} ─────────────

    [HttpPut("{linkId:guid}")]
    [SwaggerOperation(Summary = "Update redirect link", OperationId = "AdminRedirectLinks_Update")]
    [SwaggerResponse(200, "Updated redirect link.", typeof(NodeRedirectLinkResponse))]
    [SwaggerResponse(400, "Validation error.")]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Link or node not found.")]
    [SwaggerResponse(422, "Node is not a Redirect type.")]
    public async Task<IActionResult> UpdateLink(
        [FromRoute] Guid nodeId,
        [FromRoute] Guid linkId,
        [FromBody] UpdateNodeRedirectLinkRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _updateLink.ExecuteAsync(nodeId, linkId, userId, request, ct);
        return ToActionResult(result);
    }

    // ── DELETE /api/admin/nodes/{nodeId}/redirect-links/{linkId} ──────────

    [HttpDelete("{linkId:guid}")]
    [SwaggerOperation(Summary = "Delete redirect link", OperationId = "AdminRedirectLinks_Delete")]
    [SwaggerResponse(204, "Redirect link deleted.")]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Link or node not found.")]
    [SwaggerResponse(422, "Node is not a Redirect type.")]
    public async Task<IActionResult> DeleteLink(
        [FromRoute] Guid nodeId,
        [FromRoute] Guid linkId,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _deleteLink.ExecuteAsync(nodeId, linkId, userId, ct);

        if (!result.Success)
            return ToActionResult(result);

        return NoContent();
    }

    // ── PUT /api/admin/nodes/{nodeId}/redirect-links/reorder ──────────────

    [HttpPut("reorder")]
    [SwaggerOperation(Summary = "Reorder redirect links", OperationId = "AdminRedirectLinks_Reorder")]
    [SwaggerResponse(200, "Redirect links reordered.", typeof(List<NodeRedirectLinkResponse>))]
    [SwaggerResponse(400, "Validation error.")]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Node not found.")]
    [SwaggerResponse(422, "Node is not a Redirect type.")]
    public async Task<IActionResult> ReorderLinks(
        [FromRoute] Guid nodeId,
        [FromBody] ReorderNodeRedirectLinksRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _reorderLinks.ExecuteAsync(nodeId, userId, request, ct);
        return ToActionResult(result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private IActionResult ToActionResult<T>(FlowResult<T> result)
    {
        if (result.Success)
            return Ok(result.Data);

        return result.StatusCode switch
        {
            404 => NotFound(new { message = result.ErrorMessage }),
            409 => Conflict(new { message = result.ErrorMessage }),
            422 => UnprocessableEntity(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }
}