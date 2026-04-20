using Infrastructure.Contracts.Flows.Requests;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.UseCases.Flows;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

[ApiController]
[Route("api/admin/flows")]
[Authorize]
[Produces("application/json")]
[Tags("Admin — Flows")]
public sealed class FlowsController : ControllerBase
{
    private readonly ListFlowsUseCase _listFlows;
    private readonly GetFlowUseCase _getFlow;
    private readonly CreateFlowUseCase _createFlow;
    private readonly UpdateFlowUseCase _updateFlow;
    private readonly SetEntryNodeUseCase _setEntryNode;
    private readonly PublishFlowUseCase _publishFlow;
    private readonly UnpublishFlowUseCase _unpublishFlow;
    private readonly DeleteFlowUseCase _deleteFlow;

    public FlowsController(
        ListFlowsUseCase listFlows,
        GetFlowUseCase getFlow,
        CreateFlowUseCase createFlow,
        UpdateFlowUseCase updateFlow,
        SetEntryNodeUseCase setEntryNode,
        PublishFlowUseCase publishFlow,
        UnpublishFlowUseCase unpublishFlow,
        DeleteFlowUseCase deleteFlow)
    {
        _listFlows = listFlows;
        _getFlow = getFlow;
        _createFlow = createFlow;
        _updateFlow = updateFlow;
        _setEntryNode = setEntryNode;
        _publishFlow = publishFlow;
        _unpublishFlow = unpublishFlow;
        _deleteFlow = deleteFlow;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

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

    private bool TryGetUserId(out Guid userId)
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    // ── GET /api/admin/flows ─────────────────────────────────────────────────

    [HttpGet]
    [SwaggerOperation(Summary = "List flows", OperationId = "AdminFlows_List")]
    [SwaggerResponse(200, "Flow list.", typeof(List<FlowSummaryResponse>))]
    [SwaggerResponse(401, "Not authenticated.")]
    public async Task<IActionResult> ListFlows(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _listFlows.ExecuteAsync(userId, ct);
        return ToActionResult(result);
    }

    // ── GET /api/admin/flows/{id} ────────────────────────────────────────────

    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Get flow with full DAG", OperationId = "AdminFlows_Get")]
    [SwaggerResponse(200, "Flow detail with full DAG.", typeof(FlowDetailResponse))]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Flow not found.")]
    public async Task<IActionResult> GetFlow([FromRoute] Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _getFlow.ExecuteAsync(id, userId, ct);
        return ToActionResult(result);
    }

    // ── POST /api/admin/flows ────────────────────────────────────────────────

    [HttpPost]
    [SwaggerOperation(Summary = "Create flow", OperationId = "AdminFlows_Create")]
    [SwaggerResponse(201, "Flow created.", typeof(FlowSummaryResponse))]
    [SwaggerResponse(400, "Validation error.")]
    [SwaggerResponse(401, "Not authenticated.")]
    public async Task<IActionResult> CreateFlow(
        [FromBody] CreateFlowRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _createFlow.ExecuteAsync(request, userId, ct);

        if (!result.Success)
            return ToActionResult(result);

        return CreatedAtAction(
            actionName: nameof(GetFlow),
            routeValues: new { id = result.Data!.Id },
            value: result.Data);
    }

    // ── PUT /api/admin/flows/{id} ────────────────────────────────────────────

    [HttpPut("{id:guid}")]
    [SwaggerOperation(Summary = "Update flow", OperationId = "AdminFlows_Update")]
    [SwaggerResponse(200, "Updated flow summary.", typeof(FlowSummaryResponse))]
    [SwaggerResponse(400, "Validation error.")]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Flow not found.")]
    public async Task<IActionResult> UpdateFlow(
        [FromRoute] Guid id,
        [FromBody] UpdateFlowRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _updateFlow.ExecuteAsync(id, userId, request, ct);
        return ToActionResult(result);
    }

    // ── PUT /api/admin/flows/{id}/entry-node ─────────────────────────────────

    [HttpPut("{id:guid}/entry-node")]
    [SwaggerOperation(Summary = "Set entry node", OperationId = "AdminFlows_SetEntryNode")]
    [SwaggerResponse(200, "Updated flow summary.", typeof(FlowSummaryResponse))]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Flow not found.")]
    [SwaggerResponse(422, "Node does not belong to this flow.")]
    public async Task<IActionResult> SetEntryNode(
        [FromRoute] Guid id,
        [FromBody] SetEntryNodeRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _setEntryNode.ExecuteAsync(id, userId, request, ct);
        return ToActionResult(result);
    }

    // ── POST /api/admin/flows/{id}/publish ───────────────────────────────────

    [HttpPost("{id:guid}/publish")]
    [SwaggerOperation(Summary = "Publish flow", OperationId = "AdminFlows_Publish")]
    [SwaggerResponse(200, "Published flow summary.", typeof(FlowSummaryResponse))]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Flow not found.")]
    [SwaggerResponse(409, "Flow is already published.")]
    [SwaggerResponse(422, "Flow cannot be published.")]
    public async Task<IActionResult> PublishFlow([FromRoute] Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _publishFlow.ExecuteAsync(id, userId, ct);
        return ToActionResult(result);
    }

    // ── POST /api/admin/flows/{id}/unpublish ─────────────────────────────────

    [HttpPost("{id:guid}/unpublish")]
    [SwaggerOperation(Summary = "Unpublish flow", OperationId = "AdminFlows_Unpublish")]
    [SwaggerResponse(200, "Unpublished flow summary.", typeof(FlowSummaryResponse))]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Flow not found.")]
    [SwaggerResponse(409, "Flow is not currently published.")]
    public async Task<IActionResult> UnpublishFlow([FromRoute] Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _unpublishFlow.ExecuteAsync(id, userId, ct);
        return ToActionResult(result);
    }

    // ── DELETE /api/admin/flows/{id} ─────────────────────────────────────────

    [HttpDelete("{id:guid}")]
    [SwaggerOperation(Summary = "Delete flow", OperationId = "AdminFlows_Delete")]
    [SwaggerResponse(204, "Flow deleted.")]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Flow not found.")]
    public async Task<IActionResult> DeleteFlow([FromRoute] Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _deleteFlow.ExecuteAsync(id, userId, ct);

        if (!result.Success)
            return ToActionResult(result);

        return NoContent();
    }
}