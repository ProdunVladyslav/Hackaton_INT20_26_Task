using Application.Contracts.Analytics;
using Infrastructure.Contracts.Analytics.Responses;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.UseCases.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace Hackaton_INT20_26_Task.Controllers;

/// <summary>
/// Admin analytics endpoints.
/// Requires authentication.
/// </summary>
[ApiController]
[Route("api/admin/analytics")]
[Authorize]
[Produces("application/json")]
[Tags("Admin — Analytics")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly SessionStatsUseCase _sessionStats;
    private readonly OfferStatsUseCase _offerStats;
    private readonly DropOffUseCase _dropOff;
    private readonly LeadQualityUseCase _leadQuality;
    private readonly ChannelStatsUseCase _channelStats;

    public AnalyticsController(
        SessionStatsUseCase sessionStats,
        OfferStatsUseCase offerStats,
        DropOffUseCase dropOff,
        LeadQualityUseCase leadQuality,
        ChannelStatsUseCase channelStats)
    {
        _sessionStats = sessionStats;
        _offerStats = offerStats;
        _dropOff = dropOff;
        _leadQuality = leadQuality;
        _channelStats = channelStats;
    }

    // ── GET /api/admin/analytics/sessions ──────────────────────────────────────

    [HttpGet("sessions")]
    [SwaggerOperation(
        Summary = "Get session statistics",
        Description = "Returns overall session metrics: counts by status, completion rate, abandon rate.",
        OperationId = "Analytics_GetSessionStats")]
    [SwaggerResponse(200, "Session statistics.", typeof(SessionStatsResponse))]
    public async Task<IActionResult> GetSessionStats(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var result = await _sessionStats.ExecuteAsync(Guid.Parse(userId), ct);
        return ToActionResult(result);
    }

    // ── GET /api/admin/analytics/offers ────────────────────────────────────────

    [HttpGet("offers")]
    [SwaggerOperation(
        Summary = "Get offer statistics",
        Description = "Returns per-offer metrics: times presented, times converted, conversion rate.",
        OperationId = "Analytics_GetOfferStats")]
    [SwaggerResponse(200, "Offer statistics.", typeof(OfferStatsResponse))]
    public async Task<IActionResult> GetOfferStats(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var result = await _offerStats.ExecuteAsync(Guid.Parse(userId), ct);
        return ToActionResult(result);
    }

    // ── GET /api/admin/analytics/drop-offs ─────────────────────────────────────

    [HttpGet("drop-offs")]
    [SwaggerOperation(
        Summary = "Get drop-off analysis",
        Description = "Returns drop-off metrics per node: sessions stuck there, drop-off rate.",
        OperationId = "Analytics_GetDropOffs")]
    [SwaggerResponse(200, "Drop-off analysis.", typeof(DropOffResponse))]
    public async Task<IActionResult> GetDropOffs(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var result = await _dropOff.ExecuteAsync(Guid.Parse(userId), ct);
        return ToActionResult(result);
    }

    // ── GET /api/admin/analytics/lead-quality ──────────────────────────────────

    [HttpGet("lead-quality")]
    [SwaggerOperation(
        Summary = "Get lead quality breakdown",
        Description = "Returns tier distribution and top disqualification reasons across every flow the user owns.",
        OperationId = "Analytics_GetLeadQuality")]
    [SwaggerResponse(200, "Lead quality breakdown.", typeof(LeadQualityResponse))]
    public async Task<IActionResult> GetLeadQuality(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var result = await _leadQuality.ExecuteAsync(Guid.Parse(userId), ct);
        return ToActionResult(result);
    }

    // ── GET /api/admin/analytics/channels ───────────────────────────────────────

    [HttpGet("channels")]
    [SwaggerOperation(
        Summary = "Get channel performance",
        Description = "Returns per-channel session/qualification metrics across every flow the user owns.",
        OperationId = "Analytics_GetChannelStats")]
    [SwaggerResponse(200, "Channel performance.", typeof(ChannelStatsResponse))]
    public async Task<IActionResult> GetChannelStats(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var result = await _channelStats.ExecuteAsync(Guid.Parse(userId), ct);
        return ToActionResult(result);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

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
