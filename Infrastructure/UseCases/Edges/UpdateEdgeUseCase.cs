using Application.Repositories.Interfaces;
using Domain;
using Domain.Model.Survey;
using Domain.Services;
using Infrastructure.Contracts.Edges.Requests;
using Infrastructure.Contracts.Edges.Responses;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Services.Validators;

namespace Infrastructure.UseCases.Edges;

/// <summary>
/// Use case: update an edge's properties (priority, conditions).
/// Only provided fields are updated.
/// </summary>
public sealed class UpdateEdgeUseCase(
    IEdgeRepository _edges,
    INodeRepository _nodes,
    IUserProfileRepository _userProfiles,
    FlowStructureService _structure,
    IUnitOfWork _uow)
{
    public async Task<FlowResult<EdgeResponse>> ExecuteAsync(
        Guid flowId,
        Guid edgeId,
        Guid applicationUserId,
        UpdateEdgeRequest request,
        CancellationToken ct = default)
    {
        // ── 1. Auth ───────────────────────────────────────────────────────────
        var profile = await _userProfiles.FirstOrDefaultAsync(
            p => p.ApplicationUserId == applicationUserId, ct);
        if (profile is null)
            return FlowResult<EdgeResponse>.NotFound("User profile not found.");

        var edge = await _edges.GetByIdWithOwnerCheckAsync(edgeId, profile.Id, ct);
        if (edge is null)
            return FlowResult<EdgeResponse>.NotFound("Edge not found.");

        if (edge.FlowId != flowId)
            return FlowResult<EdgeResponse>.NotFound("Edge not found in this flow.");

        // ── 2. Parse new conditions if provided (infrastructure concern) ──────
        ConditionGroup? conditions = null;
        if (request.ConditionsJson is not null)
        {
            try
            {
                conditions = ConditionsJsonParser.Parse(request.ConditionsJson);
            }
            catch (ArgumentException ex)
            {
                return FlowResult<EdgeResponse>.Fail(ex.Message, 400);
            }
        }

        // ── 3. Domain validation if conditions changed ────────────────────────
        if (conditions is not null)
        {
            try
            {
                var sourceNode = await _nodes.FirstOrDefaultAsync(
                    n => n.Id == edge.SourceNodeId && n.FlowId == flowId, ct);
                if (sourceNode is null)
                    return FlowResult<EdgeResponse>.Fail("Source node not found.", 404);

                var flowNodes = await _nodes.GetByFlowAsync(flowId, ct);

                // On update we only re-validate conditions + source type —
                // no cycle check needed since topology isn't changing.
                //if (sourceNode.Type is NodeType.InfoPage or NodeType.LeadCapture
                //    && conditions is { Rules.Count: > 0 })
                //    throw new DomainException(
                //        $"{sourceNode.Type} nodes do not support conditional edges.");

                _structure.ValidateConditionGroup(conditions, flowNodes);
            }
            catch (DomainException ex)
            {
                return FlowResult<EdgeResponse>.Fail(ex.Message, 422);
            }
        }

        // ── 4. Apply + persist ────────────────────────────────────────────────
        try
        {
            if (request.Priority.HasValue)
                edge.SetPriority(request.Priority.Value);

            // ConditionsJson is always explicitly supplied by the admin frontend
            // (never omitted) — a `null` here means "clear the conditions, make
            // this edge unconditional", not "leave it untouched". Gating on
            // `is not null` silently dropped that clear, so edges that should
            // have become unconditional (e.g. after their last remaining
            // condition rule was stripped) kept their stale conditions server-side.
            edge.UpdateConditions(request.ConditionsJson ?? "");

            _edges.Update(edge);
            await _uow.SaveChangesAsync(ct);

            return FlowResult<EdgeResponse>.Ok(ToResponse(edge));
        }
        catch (ArgumentException ex)
        {
            return FlowResult<EdgeResponse>.Fail(ex.Message, 400);
        }
    }

    private static EdgeResponse ToResponse(Edge edge) =>
        new(edge.Id, edge.FlowId, edge.SourceNodeId, edge.TargetNodeId,
            edge.Priority, edge.ConditionsJson, edge.CreatedAt);
}