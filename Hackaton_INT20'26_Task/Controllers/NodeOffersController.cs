using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.NodeOffers.Requests;
using Infrastructure.Contracts.NodeOffers.Responses;
using Infrastructure.UseCases.NodeOffers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Hackaton_INT20_26_Task.Controllers;

/// <summary>
/// Admin endpoints for managing node-offer links.
/// All endpoints require authentication.
/// </summary>
[ApiController]
[Route("api/admin/nodes/{nodeId:guid}/offers")]
[Authorize]
[Produces("application/json")]
[Tags("Admin — Node↔Offer")]
public sealed class NodeOffersController : ControllerBase
{
    private readonly ListNodeOffersUseCase _listNodeOffers;
    private readonly LinkOfferUseCase _linkOffer;
    private readonly UpdateNodeOfferUseCase _updateNodeOffer;
    private readonly UnlinkOfferUseCase _unlinkOffer;

    public NodeOffersController(
        ListNodeOffersUseCase listNodeOffers,
        LinkOfferUseCase linkOffer,
        UpdateNodeOfferUseCase updateNodeOffer,
        UnlinkOfferUseCase unlinkOffer)
    {
        _listNodeOffers = listNodeOffers;
        _linkOffer = linkOffer;
        _updateNodeOffer = updateNodeOffer;
        _unlinkOffer = unlinkOffer;
    }

    // ── GET /api/admin/nodes/{nodeId}/offers ──────────────────────────────────

    [HttpGet]
    [SwaggerOperation(
        Summary = "List node offers",
        Description = "Returns all offers linked to a specific node.",
        OperationId = "AdminNodeOffers_List")]
    [SwaggerResponse(200, "Node offer list.", typeof(List<NodeOfferResponse>))]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Node not found.")]
    public async Task<IActionResult> ListNodeOffers([FromRoute] Guid nodeId, CancellationToken ct)
    {
        var result = await _listNodeOffers.ExecuteAsync(nodeId, ct);
        return ToActionResult(result);
    }

    // ── POST /api/admin/nodes/{nodeId}/offers ─────────────────────────────────

    [HttpPost]
    [SwaggerOperation(
        Summary = "Link offer to node",
        Description = "Links an offer to a node. Offer and node must exist and must not already be linked.",
        OperationId = "AdminNodeOffers_Link")]
    [SwaggerResponse(201, "Offer linked.", typeof(NodeOfferResponse))]
    [SwaggerResponse(400, "Validation error.")]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "Node or offer not found.")]
    [SwaggerResponse(409, "Offer already linked to this node.")]
    public async Task<IActionResult> LinkOffer(
        [FromRoute] Guid nodeId,
        [FromBody] LinkOfferRequest request,
        CancellationToken ct)
    {
        var result = await _linkOffer.ExecuteAsync(nodeId, request, ct);

        if (!result.Success)
            return ToActionResult(result);

        return CreatedAtAction(
            actionName: nameof(ListNodeOffers),
            routeValues: new { nodeId },
            value: result.Data);
    }

    // ── PUT /api/admin/nodes/{nodeId}/offers/{nodeOfferId} ────────────────────

    [HttpPut("{nodeOfferId:guid}")]
    [SwaggerOperation(
        Summary = "Update node offer",
        Description = "Updates a node-offer link (e.g., primary flag).",
        OperationId = "AdminNodeOffers_Update")]
    [SwaggerResponse(200, "Node offer updated.", typeof(NodeOfferResponse))]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "NodeOffer not found.")]
    public async Task<IActionResult> UpdateNodeOffer(
        [FromRoute] Guid nodeId,
        [FromRoute] Guid nodeOfferId,
        [FromBody] UpdateNodeOfferRequest request,
        CancellationToken ct)
    {
        var result = await _updateNodeOffer.ExecuteAsync(nodeId, nodeOfferId, request, ct);
        return ToActionResult(result);
    }

    // ── DELETE /api/admin/nodes/{nodeId}/offers/{nodeOfferId} ──────────────────

    [HttpDelete("{nodeOfferId:guid}")]
    [SwaggerOperation(
        Summary = "Unlink offer from node",
        Description = "Removes the link between a node and an offer.",
        OperationId = "AdminNodeOffers_Unlink")]
    [SwaggerResponse(204, "Offer unlinked.")]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "NodeOffer not found.")]
    public async Task<IActionResult> UnlinkOffer(
        [FromRoute] Guid nodeId,
        [FromRoute] Guid nodeOfferId,
        CancellationToken ct)
    {
        var result = await _unlinkOffer.ExecuteAsync(nodeId, nodeOfferId, ct);

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
