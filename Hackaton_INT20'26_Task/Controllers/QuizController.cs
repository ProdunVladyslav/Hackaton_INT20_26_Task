using Infrastructure.Contracts.Quiz.Requests;
using Infrastructure.Contracts.Quiz.Responses;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.UseCases.Quiz;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Hackaton_INT20_26_Task.Controllers;

/// <summary>
/// Public endpoints for the quiz engine.
/// These endpoints do not require authentication.
/// </summary>
[ApiController]
[Route("api/quiz")]
[AllowAnonymous]
[Produces("application/json")]
[Tags("Quiz Engine")]
public sealed class QuizController : ControllerBase
{
    private readonly StartSessionUseCase _startSession;
    private readonly GetSessionUseCase _getSession;
    private readonly SubmitAnswerUseCase _submitAnswer;
    private readonly GoBackUseCase _goBack;
    private readonly ConvertUseCase _convert;

    public QuizController(
        StartSessionUseCase startSession,
        GetSessionUseCase getSession,
        SubmitAnswerUseCase submitAnswer,
        GoBackUseCase goBack,
        ConvertUseCase convert)
    {
        _startSession = startSession;
        _getSession = getSession;
        _submitAnswer = submitAnswer;
        _goBack = goBack;
        _convert = convert;
    }

    // ── POST /api/quiz/sessions ────────────────────────────────────────────────

    [HttpPost("sessions")]
    [SwaggerOperation(
        Summary = "Start a new quiz session",
        Description = "Initiates a new session for a published flow.",
        OperationId = "Quiz_StartSession")]
    [SwaggerResponse(201, "Session started.", typeof(SessionStateResponse))]
    [SwaggerResponse(404, "Flow not found.")]
    [SwaggerResponse(422, "Flow not published or has no entry node.")]
    public async Task<IActionResult> StartSession([FromBody] StartSessionRequest request, CancellationToken ct)
    {
        var result = await _startSession.ExecuteAsync(request, ct);
        if (!result.Success)
            return ToActionResult(result);
        return CreatedAtAction(nameof(GetSession), new { id = result.Data!.SessionId }, result.Data);
    }

    // ── GET /api/quiz/sessions/{id} ────────────────────────────────────────────

    [HttpGet("sessions/{id:guid}")]
    [SwaggerOperation(
        Summary = "Get session state",
        Description = "Returns the current state of a quiz session.",
        OperationId = "Quiz_GetSession")]
    [SwaggerResponse(200, "Session state.", typeof(SessionStateResponse))]
    [SwaggerResponse(404, "Session not found.")]
    public async Task<IActionResult> GetSession([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _getSession.ExecuteAsync(id, ct);
        return ToActionResult(result);
    }

    // ── POST /api/quiz/sessions/{id}/answers ───────────────────────────────────

    [HttpPost("sessions/{id:guid}/answers")]
    [SwaggerOperation(
        Summary = "Submit an answer",
        Description = "Records an answer to the current quiz node and advances to the next node.",
        OperationId = "Quiz_SubmitAnswer")]
    [SwaggerResponse(200, "Answer recorded, session advanced.", typeof(SessionStateResponse))]
    [SwaggerResponse(404, "Session or node not found.")]
    [SwaggerResponse(422, "Session not active or node mismatch.")]
    public async Task<IActionResult> SubmitAnswer([FromRoute] Guid id, [FromBody] SubmitAnswerRequest request, CancellationToken ct)
    {
        var result = await _submitAnswer.ExecuteAsync(id, request, ct);
        return ToActionResult(result);
    }

    // ── POST /api/quiz/sessions/{id}/back ──────────────────────────────────────

    [HttpPost("sessions/{id:guid}/back")]
    [SwaggerOperation(
        Summary = "Go back to previous node",
        Description = "Moves the session back to the previous node and removes the last answer.",
        OperationId = "Quiz_GoBack")]
    [SwaggerResponse(200, "Session moved back.", typeof(SessionStateResponse))]
    [SwaggerResponse(404, "Session not found.")]
    [SwaggerResponse(422, "Session not active or already at beginning.")]
    public async Task<IActionResult> GoBack([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _goBack.ExecuteAsync(id, ct);
        return ToActionResult(result);
    }

    // ── POST /api/quiz/sessions/{id}/convert ───────────────────────────────────

    [HttpPost("sessions/{id:guid}/convert")]
    [SwaggerOperation(
        Summary = "Convert an offer",
        Description = "Marks an offer as converted (user clicked CTA / purchased).",
        OperationId = "Quiz_Convert")]
    [SwaggerResponse(200, "Offer converted.")]
    [SwaggerResponse(404, "Session or offer not found.")]
    public async Task<IActionResult> Convert([FromRoute] Guid id, [FromBody] ConvertRequest request, CancellationToken ct)
    {
        var result = await _convert.ExecuteAsync(id, request, ct);
        if (result.Success)
            return Ok();
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
