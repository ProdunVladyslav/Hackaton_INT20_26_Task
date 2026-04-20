using Infrastructure.Contracts.AIGeneration.Requests;
using Infrastructure.Contracts.AIGeneration.Responses;
using Infrastructure.UseCases.AIGeneration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace Hackaton_INT20_26_Task.Controllers
{
    /// <summary>
    /// Admin endpoint for AI-assisted flow generation.
    /// Accepts a natural language prompt and orchestrates the full flow DAG creation.
    /// </summary>
    [ApiController]
    [Route("api/admin/flows/generate")]
    [Authorize]
    [Produces("application/json")]
    [Tags("Admin — AI Generation")]
    public sealed class GenerateFlowController : ControllerBase
    {
        private readonly StartGenerateFlowUseCase _start;
        private readonly GetGenerateFlowStatusUseCase _status;

        public GenerateFlowController(
            StartGenerateFlowUseCase start,
            GetGenerateFlowStatusUseCase status)
        {
            _start = start;
            _status = status;
        }

        // ── POST /api/admin/flows/generate ────────────────────────────────────────
        [HttpPost]
        [SwaggerOperation(
            Summary = "Start flow generation (async)",
            OperationId = "AdminAI_StartGenerateFlow")]
        [SwaggerResponse(202, "Job accepted.", typeof(GenerateFlowJobAcceptedResponse))]
        [SwaggerResponse(400, "Invalid request.")]
        [SwaggerResponse(401, "Not authenticated.")]
        public IActionResult StartGenerate([FromBody] GenerateFlowRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserPrompt))
                return BadRequest(new { message = "UserPrompt is required." });

            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Unauthorized();

            var jobId = _start.Execute(request, userId);
            return Accepted(new GenerateFlowJobAcceptedResponse(jobId));
        }

        // ── GET /api/admin/flows/generate/status/{jobId} ──────────────────────────
        [HttpGet("status/{jobId:guid}")]
        [SwaggerOperation(
            Summary = "Poll generation job status",
            Description = "Returns Pending/Running/Done/Failed. When Done, includes the created flowId.",
            OperationId = "AdminAI_GetGenerateFlowStatus")]
        [SwaggerResponse(200, "Job found.", typeof(GenerateFlowStatusResponse))]
        [SwaggerResponse(404, "Job not found.")]
        [SwaggerResponse(401, "Not authenticated.")]
        public IActionResult GetStatus(Guid jobId)
        {
            var result = _status.Execute(jobId);
            return result is null ? NotFound(new { message = $"Job {jobId} not found." }) : Ok(result);
        }
    }
}
