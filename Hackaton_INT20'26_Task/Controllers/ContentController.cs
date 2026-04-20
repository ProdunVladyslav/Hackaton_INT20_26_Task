using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.UseCases.Content;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Hackaton_INT20_26_Task.Controllers;

/// <summary>
/// Public endpoints for content delivery.
/// These endpoints do not require authentication.
/// </summary>
[ApiController]
[Route("api/content")]
[AllowAnonymous]
[Produces("application/json")]
[Tags("Content Delivery")]
public sealed class ContentController(
    GetPublishedFlowUseCase _getPublishedFlow,
    GetPublishedFlowByIdUseCase _getPublishedFlowById) : ControllerBase
{
    // ── GET /api/content/flow ─────────────────────────────────────────────────

    [HttpGet("flow")]
    [SwaggerOperation(
        Summary = "Get published flow",
        Description = "Returns the first published flow (newest) with full DAG. Used by end-users to access content.",
        OperationId = "Content_GetFlow")]
    [SwaggerResponse(200, "Published flow detail.", typeof(FlowDetailResponse))]
    [SwaggerResponse(404, "No published flow available.")]
    public async Task<IActionResult> GetPublishedFlow(CancellationToken ct)
    {
        var result = await _getPublishedFlow.ExecuteAsync(ct);
        return ToActionResult(result);
    }

    // ── GET /api/content/flow/{id} ────────────────────────────────────────────

    [HttpGet("flow/{id:guid}")]
    [SwaggerOperation(
        Summary = "Get published flow by ID",
        Description = "Returns a specific published flow by ID with full DAG.",
        OperationId = "Content_GetFlowById")]
    [SwaggerResponse(200, "Published flow detail.", typeof(FlowDetailResponse))]
    [SwaggerResponse(404, "Published flow not found.")]
    public async Task<IActionResult> GetPublishedFlowById([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _getPublishedFlowById.ExecuteAsync(id, ct);
        return ToActionResult(result);
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
            _   => BadRequest(new { message = result.ErrorMessage })
        };
    }
}
