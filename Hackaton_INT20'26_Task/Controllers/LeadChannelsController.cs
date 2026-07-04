using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.LeadChannels.Requests;
using Infrastructure.Contracts.LeadChannels.Responses;
using Infrastructure.UseCases.LeadChannels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace Hackaton_INT20_26_Task.Controllers;

/// <summary>
/// Admin CRUD for a flow's trackable share links (lead channels).
/// All endpoints require authentication and are scoped to the caller's own flow.
/// </summary>
[ApiController]
[Route("api/admin/flows/{flowId:guid}/lead-channels")]
[Authorize]
[Produces("application/json")]
[Tags("Admin — Lead Channels")]
public sealed class LeadChannelsController : ControllerBase
{
    private readonly CreateLeadChannelUseCase _createChannel;
    private readonly ListLeadChannelsUseCase _listChannels;
    private readonly UpdateLeadChannelUseCase _updateChannel;
    private readonly DeleteLeadChannelUseCase _deleteChannel;

    public LeadChannelsController(
        CreateLeadChannelUseCase createChannel,
        ListLeadChannelsUseCase listChannels,
        UpdateLeadChannelUseCase updateChannel,
        DeleteLeadChannelUseCase deleteChannel)
    {
        _createChannel = createChannel;
        _listChannels = listChannels;
        _updateChannel = updateChannel;
        _deleteChannel = deleteChannel;
    }

    private bool TryGetUserId(out Guid userId)
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    // ── GET /api/admin/flows/{flowId}/lead-channels ───────────────────────────

    [HttpGet]
    [SwaggerOperation(
        Summary = "List lead channels for a flow",
        Description = "Returns every channel the owner has created, with session/qualification counts.",
        OperationId = "AdminLeadChannels_List")]
    [SwaggerResponse(200, "Channel list.", typeof(List<LeadChannelResponse>))]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Flow not found.")]
    public async Task<IActionResult> ListChannels([FromRoute] Guid flowId, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _listChannels.ExecuteAsync(flowId, userId, ct);
        return ToActionResult(result);
    }

    // ── POST /api/admin/flows/{flowId}/lead-channels ──────────────────────────

    [HttpPost]
    [SwaggerOperation(
        Summary = "Create a lead channel",
        Description = "Generates a unique 8-character short code server-side — the caller only supplies a name.",
        OperationId = "AdminLeadChannels_Create")]
    [SwaggerResponse(201, "Channel created.", typeof(LeadChannelResponse))]
    [SwaggerResponse(400, "Validation error.")]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Flow not found.")]
    public async Task<IActionResult> CreateChannel(
        [FromRoute] Guid flowId,
        [FromBody] CreateLeadChannelRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _createChannel.ExecuteAsync(flowId, userId, request, ct);

        if (!result.Success)
            return ToActionResult(result);

        return CreatedAtAction(
            actionName: nameof(ListChannels),
            routeValues: new { flowId },
            value: result.Data);
    }

    // ── PATCH /api/admin/flows/{flowId}/lead-channels/{channelId} ─────────────

    [HttpPatch("{channelId:guid}")]
    [SwaggerOperation(
        Summary = "Rename or archive/restore a lead channel",
        Description = "Patch semantics — only provided fields are updated.",
        OperationId = "AdminLeadChannels_Update")]
    [SwaggerResponse(200, "Updated channel.", typeof(LeadChannelResponse))]
    [SwaggerResponse(400, "Validation error.")]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Channel not found.")]
    public async Task<IActionResult> UpdateChannel(
        [FromRoute] Guid flowId,
        [FromRoute] Guid channelId,
        [FromBody] UpdateLeadChannelRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _updateChannel.ExecuteAsync(flowId, channelId, userId, request, ct);
        return ToActionResult(result);
    }

    // ── DELETE /api/admin/flows/{flowId}/lead-channels/{channelId} ────────────

    [HttpDelete("{channelId:guid}")]
    [SwaggerOperation(
        Summary = "Delete a lead channel",
        Description = "Only succeeds if no sessions have been attributed to it yet — archive it instead once it has history.",
        OperationId = "AdminLeadChannels_Delete")]
    [SwaggerResponse(204, "Channel deleted.")]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Channel not found.")]
    [SwaggerResponse(409, "Channel has attributed sessions — archive instead.")]
    public async Task<IActionResult> DeleteChannel(
        [FromRoute] Guid flowId,
        [FromRoute] Guid channelId,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _deleteChannel.ExecuteAsync(flowId, channelId, userId, ct);

        if (!result.Success)
            return ToActionResult(result);

        return NoContent();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private IActionResult ToActionResult<T>(FlowResult<T> result)
    {
        if (result.Success) return Ok(result.Data);

        return result.StatusCode switch
        {
            404 => NotFound(new { message = result.ErrorMessage }),
            409 => Conflict(new { message = result.ErrorMessage }),
            422 => UnprocessableEntity(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }
}
