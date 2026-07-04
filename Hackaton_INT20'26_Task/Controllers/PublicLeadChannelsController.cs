using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.LeadChannels.Responses;
using Infrastructure.UseCases.LeadChannels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Hackaton_INT20_26_Task.Controllers;

/// <summary>
/// Public endpoint for resolving a short-link code clicked by a respondent.
/// Does not require authentication.
/// </summary>
[ApiController]
[Route("api/public/lead-channels")]
[AllowAnonymous]
[Produces("application/json")]
[Tags("Public — Lead Channels")]
public sealed class PublicLeadChannelsController : ControllerBase
{
    private readonly ResolveLeadChannelUseCase _resolveChannel;

    public PublicLeadChannelsController(ResolveLeadChannelUseCase resolveChannel)
    {
        _resolveChannel = resolveChannel;
    }

    // ── GET /api/public/lead-channels/{shortCode} ─────────────────────────────

    [HttpGet("{shortCode}")]
    [SwaggerOperation(
        Summary = "Resolve a short-link code",
        Description = "Returns which flow and channel a short code points to, so the client can redirect into the survey.",
        OperationId = "PublicLeadChannels_Resolve")]
    [SwaggerResponse(200, "Resolved.", typeof(LeadChannelResolveResponse))]
    [SwaggerResponse(404, "Link not found, archived, or flow unpublished.")]
    public async Task<IActionResult> Resolve([FromRoute] string shortCode, CancellationToken ct)
    {
        var result = await _resolveChannel.ExecuteAsync(shortCode, ct);

        if (!result.Success)
            return NotFound(new { message = result.ErrorMessage });

        return Ok(result.Data);
    }
}
