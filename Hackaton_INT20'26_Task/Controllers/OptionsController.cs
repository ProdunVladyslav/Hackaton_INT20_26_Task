using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.Options.Requests;
using Infrastructure.Contracts.Options.Responses;
using Infrastructure.UseCases.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace Hackaton_INT20_26_Task.Controllers;

/// <summary>
/// Admin endpoints for managing options within nodes.
/// All endpoints require an authenticated session (cookie).
/// </summary>
[ApiController]
[Route("api/admin/nodes/{nodeId:guid}/options")]
[Authorize]
[Produces("application/json")]
[Tags("Admin — Options")]
public sealed class OptionsController : ControllerBase
{
    private readonly CreateOptionUseCase _createOption;
    private readonly UpdateOptionUseCase _updateOption;
    private readonly DeleteOptionUseCase _deleteOption;
    private readonly ReorderOptionsUseCase _reorderOptions;

    public OptionsController(
        CreateOptionUseCase createOption,
        UpdateOptionUseCase updateOption,
        DeleteOptionUseCase deleteOption,
        ReorderOptionsUseCase reorderOptions)
    {
        _createOption = createOption;
        _updateOption = updateOption;
        _deleteOption = deleteOption;
        _reorderOptions = reorderOptions;
    }

    private bool TryGetUserId(out Guid userId)
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    // ── POST /api/admin/nodes/{nodeId}/options ────────────────────────────

    [HttpPost]
    [SwaggerOperation(Summary = "Create option", OperationId = "AdminOptions_Create")]
    [SwaggerResponse(201, "Option created.", typeof(OptionResponse))]
    [SwaggerResponse(400, "Validation error.")]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Node not found.")]
    [SwaggerResponse(422, "Node is not a question type.")]
    public async Task<IActionResult> CreateOption(
        [FromRoute] Guid nodeId,
        [FromBody] CreateOptionRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _createOption.ExecuteAsync(nodeId, userId, request, ct);

        if (!result.Success)
            return ToActionResult(result);

        return StatusCode(201, result.Data);
    }

    // ── PUT /api/admin/nodes/{nodeId}/options/{optionId} ──────────────────

    [HttpPut("{optionId:guid}")]
    [SwaggerOperation(Summary = "Update option", OperationId = "AdminOptions_Update")]
    [SwaggerResponse(200, "Updated option.", typeof(OptionResponse))]
    [SwaggerResponse(400, "Validation error.")]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Option or node not found.")]
    public async Task<IActionResult> UpdateOption(
        [FromRoute] Guid nodeId,
        [FromRoute] Guid optionId,
        [FromBody] UpdateOptionRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _updateOption.ExecuteAsync(nodeId, optionId, userId, request, ct);
        return ToActionResult(result);
    }

    // ── DELETE /api/admin/nodes/{nodeId}/options/{optionId} ───────────────

    [HttpDelete("{optionId:guid}")]
    [SwaggerOperation(Summary = "Delete option", OperationId = "AdminOptions_Delete")]
    [SwaggerResponse(204, "Option deleted.")]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Option or node not found.")]
    public async Task<IActionResult> DeleteOption(
        [FromRoute] Guid nodeId,
        [FromRoute] Guid optionId,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _deleteOption.ExecuteAsync(nodeId, optionId, userId, ct);

        if (!result.Success)
            return ToActionResult(result);

        return NoContent();
    }

    // ── PUT /api/admin/nodes/{nodeId}/options/reorder ─────────────────────

    [HttpPut("reorder")]
    [SwaggerOperation(Summary = "Reorder options", OperationId = "AdminOptions_Reorder")]
    [SwaggerResponse(200, "Options reordered.", typeof(List<OptionResponse>))]
    [SwaggerResponse(400, "Validation error.")]
    [SwaggerResponse(401, "Not authenticated.")]
    public async Task<IActionResult> ReorderOptions(
        [FromRoute] Guid nodeId,
        [FromBody] ReorderOptionsRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _reorderOptions.ExecuteAsync(nodeId, userId, request, ct);
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
