using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.Offers.Requests;
using Infrastructure.Contracts.Offers.Responses;
using Infrastructure.UseCases.Offers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Hackaton_INT20_26_Task.Controllers;

/// <summary>
/// Admin endpoints for managing offers.
/// All endpoints require authentication.
/// </summary>
[ApiController]
[Route("api/admin/offers")]
[Authorize]
[Produces("application/json")]
[Tags("Admin — Offers")]
public sealed class OffersController : ControllerBase
{
    private readonly ListOffersUseCase _listOffers;
    private readonly GetOfferUseCase _getOffer;
    private readonly CreateOfferUseCase _createOffer;
    private readonly UpdateOfferUseCase _updateOffer;
    private readonly DeleteOfferUseCase _deleteOffer;

    public OffersController(
        ListOffersUseCase listOffers,
        GetOfferUseCase getOffer,
        CreateOfferUseCase createOffer,
        UpdateOfferUseCase updateOffer,
        DeleteOfferUseCase deleteOffer)
    {
        _listOffers = listOffers;
        _getOffer = getOffer;
        _createOffer = createOffer;
        _updateOffer = updateOffer;
        _deleteOffer = deleteOffer;
    }

    // ── GET /api/admin/offers ─────────────────────────────────────────────────

    [HttpGet]
    [SwaggerOperation(
        Summary = "List all offers",
        Description = "Returns all offers ordered by name.",
        OperationId = "AdminOffers_List")]
    [SwaggerResponse(200, "Offer list.", typeof(List<OfferResponse>))]
    [SwaggerResponse(401, "Not authenticated.")]
    public async Task<IActionResult> ListOffers(CancellationToken ct)
    {
        var result = await _listOffers.ExecuteAsync(ct);
        return Ok(result.Data);
    }

    // ── GET /api/admin/offers/{id} ────────────────────────────────────────────

    [HttpGet("{id:guid}")]
    [SwaggerOperation(
        Summary = "Get offer details",
        Description = "Returns a single offer by ID.",
        OperationId = "AdminOffers_Get")]
    [SwaggerResponse(200, "Offer detail.", typeof(OfferResponse))]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Offer not found.")]
    public async Task<IActionResult> GetOffer([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _getOffer.ExecuteAsync(id, ct);
        return ToActionResult(result);
    }

    // ── POST /api/admin/offers ────────────────────────────────────────────────

    [HttpPost]
    [SwaggerOperation(
        Summary = "Create offer",
        Description = "Creates a new offer with the provided details.",
        OperationId = "AdminOffers_Create")]
    [SwaggerResponse(201, "Offer created.", typeof(OfferResponse))]
    [SwaggerResponse(400, "Validation error.")]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(409, "Slug already exists.")]
    public async Task<IActionResult> CreateOffer(
        [FromBody] CreateOfferRequest request,
        CancellationToken ct)
    {
        var result = await _createOffer.ExecuteAsync(request, ct);

        if (!result.Success)
            return ToActionResult(result);

        return CreatedAtAction(
            actionName: nameof(GetOffer),
            routeValues: new { id = result.Data!.Id },
            value: result.Data);
    }

    // ── PUT /api/admin/offers/{id} ────────────────────────────────────────────

    [HttpPut("{id:guid}")]
    [SwaggerOperation(
        Summary = "Update offer",
        Description = "Updates an offer. Only provided fields are changed.",
        OperationId = "AdminOffers_Update")]
    [SwaggerResponse(200, "Updated offer.", typeof(OfferResponse))]
    [SwaggerResponse(400, "Validation error.")]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Offer not found.")]
    [SwaggerResponse(409, "Slug already exists.")]
    public async Task<IActionResult> UpdateOffer(
        [FromRoute] Guid id,
        [FromBody] UpdateOfferRequest request,
        CancellationToken ct)
    {
        var result = await _updateOffer.ExecuteAsync(id, request, ct);
        return ToActionResult(result);
    }

    // ── DELETE /api/admin/offers/{id} ─────────────────────────────────────────

    [HttpDelete("{id:guid}")]
    [SwaggerOperation(
        Summary = "Delete offer",
        Description = "Permanently deletes an offer. Offer must not be linked to any nodes.",
        OperationId = "AdminOffers_Delete")]
    [SwaggerResponse(204, "Offer deleted.")]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Offer not found.")]
    [SwaggerResponse(409, "Offer is linked to nodes.")]
    public async Task<IActionResult> DeleteOffer([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _deleteOffer.ExecuteAsync(id, ct);

        if (!result.Success)
            return ToActionResult(result);

        return NoContent();
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
