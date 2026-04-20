using Application.Contracts.Leads;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.Leads.Requests;
using Infrastructure.Contracts.Leads.Responses;
using Infrastructure.UseCases.Leads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

[ApiController]
[Route("api/admin/flows/{flowId:guid}/leads")]
[Authorize]
[Produces("application/json")]
[Tags("Admin — Leads")]
public sealed class LeadsController : ControllerBase
{
    private readonly ListLeadsUseCase _listLeads;
    private readonly GetLeadUseCase _getLead;
    private readonly UpdateLeadUseCase _updateLead;

    public LeadsController(
        ListLeadsUseCase listLeads,
        GetLeadUseCase getLead,
        UpdateLeadUseCase updateLead)
    {
        _listLeads = listLeads;
        _getLead = getLead;
        _updateLead = updateLead;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

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

    private bool TryGetUserId(out Guid userId)
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    // ── GET /api/admin/flows/{flowId}/leads ───────────────────────────────────

    [HttpGet]
    [SwaggerOperation(
        Summary = "List leads for a flow",
        Description = "Returns all leads captured by this flow. Supports filtering by tier, status, date range, free-text search, and sorting.",
        OperationId = "AdminLeads_List")]
    [SwaggerResponse(200, "Paged lead list.", typeof(PagedLeadResponse))]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Flow not found.")]
    public async Task<IActionResult> ListLeads(
        [FromRoute] Guid flowId,
        [FromQuery] string? tier = null,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var request = new ListLeadsRequest(
            Tier: tier,
            Status: status,
            Search: search,
            From: from,
            To: to,
            SortBy: sortBy,
            SortDescending: sortDescending,
            Page: page,
            PageSize: pageSize);

        var result = await _listLeads.ExecuteAsync(flowId, userId, request, ct);
        return ToActionResult(result);
    }

    // ── GET /api/admin/flows/{flowId}/leads/{leadId} ──────────────────────────

    [HttpGet("{leadId:guid}")]
    [SwaggerOperation(
        Summary = "Get lead detail",
        OperationId = "AdminLeads_Get")]
    [SwaggerResponse(200, "Lead detail.", typeof(LeadResponse))]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Lead not found.")]
    public async Task<IActionResult> GetLead(
        [FromRoute] Guid flowId,
        [FromRoute] Guid leadId,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _getLead.ExecuteAsync(leadId, userId, ct);
        return ToActionResult(result);
    }

    // ── PATCH /api/admin/flows/{flowId}/leads/{leadId} ────────────────────────

    [HttpPatch("{leadId:guid}")]
    [SwaggerOperation(
        Summary = "Update lead status, tier, notes, or assignment",
        Description = "Patch semantics — only provided fields are updated. To clear assignment pass UpdateAssignment=true with AssignedToId=null.",
        OperationId = "AdminLeads_Update")]
    [SwaggerResponse(200, "Updated lead.", typeof(LeadResponse))]
    [SwaggerResponse(400, "Validation error.")]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Lead not found.")]
    public async Task<IActionResult> UpdateLead(
        [FromRoute] Guid flowId,
        [FromRoute] Guid leadId,
        [FromBody] UpdateLeadRequest request,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _updateLead.ExecuteAsync(leadId, userId, request, ct);
        return ToActionResult(result);
    }
}