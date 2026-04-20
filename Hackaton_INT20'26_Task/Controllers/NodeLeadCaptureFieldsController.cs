using Domain.Model.Survey;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.NodeLeadCaptureFields.Requests;
using Infrastructure.Contracts.NodeLeadCaptureFields.Responses;
using Infrastructure.UseCases.NodeLeadCaptureFields;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace Hackaton_INT20_26_Task.Controllers;

/// <summary>
/// Admin endpoints for managing lead capture fields within LeadCapture nodes.
/// Fields are identified by their unique FieldType (e.g. Email, FullName).
/// All endpoints require an authenticated session (cookie).
/// </summary>
[ApiController]
[Route("api/admin/nodes/{nodeId:guid}/lead-capture-fields")]
[Authorize]
[Produces("application/json")]
[Tags("Admin — Lead Capture Fields")]
public sealed class NodeLeadCaptureFieldsController : ControllerBase
{
    private readonly CreateNodeLeadCaptureFieldUseCase _create;
    private readonly UpdateNodeLeadCaptureFieldUseCase _update;
    private readonly DeleteNodeLeadCaptureFieldUseCase _delete;
    private readonly ReorderNodeLeadCaptureFieldsUseCase _reorder;

    public NodeLeadCaptureFieldsController(
        CreateNodeLeadCaptureFieldUseCase create,
        UpdateNodeLeadCaptureFieldUseCase update,
        DeleteNodeLeadCaptureFieldUseCase delete,
        ReorderNodeLeadCaptureFieldsUseCase reorder)
    {
        _create = create;
        _update = update;
        _delete = delete;
        _reorder = reorder;
    }

    private bool TryGetUserId(out Guid userId)
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    // ── POST /api/admin/nodes/{nodeId}/lead-capture-fields ────────────────

    [HttpPost]
    [SwaggerOperation(Summary = "Add lead capture field", OperationId = "AdminLeadCaptureFields_Create")]
    [SwaggerResponse(201, "Field added.", typeof(NodeLeadCaptureFieldResponse))]
    [SwaggerResponse(400, "Validation error (e.g. Email marked not required, negative order).")]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Node not found.")]
    [SwaggerResponse(422, "Node is not LeadCapture type, FieldType already exists, or max 7 fields reached.")]
    public async Task<IActionResult> CreateField(
        [FromRoute] Guid nodeId,
        [FromBody] CreateNodeLeadCaptureFieldRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _create.ExecuteAsync(nodeId, userId, request, ct);

        if (!result.Success)
            return ToActionResult(result);

        return StatusCode(201, result.Data);
    }

    // ── PUT /api/admin/nodes/{nodeId}/lead-capture-fields/{fieldType} ─────

    [HttpPut("{fieldType}")]
    [SwaggerOperation(Summary = "Update lead capture field", OperationId = "AdminLeadCaptureFields_Update")]
    [SwaggerResponse(200, "Updated field.", typeof(NodeLeadCaptureFieldResponse))]
    [SwaggerResponse(400, "Validation error.")]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Field or node not found.")]
    [SwaggerResponse(422, "Node is not LeadCapture type.")]
    public async Task<IActionResult> UpdateField(
        [FromRoute] Guid nodeId,
        [FromRoute] string fieldType,
        [FromBody] UpdateNodeLeadCaptureFieldRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _update.ExecuteAsync(nodeId, Enum.Parse<LeadCaptureFieldType>(fieldType), userId, request, ct);
        return ToActionResult(result);
    }

    // ── DELETE /api/admin/nodes/{nodeId}/lead-capture-fields/{fieldType} ──

    [HttpDelete("{fieldType}")]
    [SwaggerOperation(Summary = "Remove lead capture field", OperationId = "AdminLeadCaptureFields_Delete")]
    [SwaggerResponse(204, "Field removed.")]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Field or node not found.")]
    [SwaggerResponse(422, "Node is not LeadCapture type.")]
    public async Task<IActionResult> DeleteField(
        [FromRoute] Guid nodeId,
        [FromRoute] string fieldType,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _delete.ExecuteAsync(nodeId, Enum.Parse<LeadCaptureFieldType>(fieldType), userId, ct);

        if (!result.Success)
            return ToActionResult(result);

        return NoContent();
    }

    // ── PUT /api/admin/nodes/{nodeId}/lead-capture-fields/reorder ─────────

    [HttpPut("reorder")]
    [SwaggerOperation(Summary = "Reorder lead capture fields", OperationId = "AdminLeadCaptureFields_Reorder")]
    [SwaggerResponse(200, "Fields reordered.", typeof(List<NodeLeadCaptureFieldResponse>))]
    [SwaggerResponse(400, "Validation error.")]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Node not found.")]
    [SwaggerResponse(422, "Node is not LeadCapture type.")]
    public async Task<IActionResult> ReorderFields(
        [FromRoute] Guid nodeId,
        [FromBody] ReorderNodeLeadCaptureFieldsRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _reorder.ExecuteAsync(nodeId, userId, request, ct);
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