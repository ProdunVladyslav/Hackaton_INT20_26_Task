using Application.Repositories.Implementations;
using Application.Repositories.Interfaces;
using Domain;
using Domain.Model.Survey;
using Domain.Services;
using Infrastructure.Contracts.Analytics.Responses;
using Infrastructure.Contracts.Edges.Requests;
using Infrastructure.Contracts.Edges.Responses;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Services.Validators;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.UseCases.Edges;

/// <summary>
/// Use case: create a new edge (connection) between two nodes in a flow.
/// 
/// Responsibilities:
///   1. Auth + ownership
///   2. Node existence checks (infrastructure)
///   3. Duplicate edge check (infrastructure)
///   4. JSON parsing (infrastructure)
///   5. Domain validation via FlowStructureService
///   6. Persist
/// </summary>
public sealed class CreateEdgeUseCase(
    IFlowRepository _flows,
    INodeRepository _nodes,
    IEdgeRepository _edges,
    IUserProfileRepository _userProfiles,
    FlowStructureService _structure,
    IUnitOfWork _uow)
{
    public async Task<FlowResult<EdgeResponse>> ExecuteAsync(
        Guid flowId,
        Guid applicationUserId,
        CreateEdgeRequest request,
        CancellationToken ct = default)
    {
        // ── 1. Auth + ownership ───────────────────────────────────────────────
        var profile = await _userProfiles.FirstOrDefaultAsync(
            p => p.ApplicationUserId == applicationUserId, ct);
        if (profile is null)
            return FlowResult<EdgeResponse>.NotFound("User profile not found.");

        var flow = await _flows.FirstOrDefaultAsync(
            x => x.Id == flowId && x.OwnerId == profile.Id, ct);
        if (flow is null)
            return FlowResult<EdgeResponse>.NotFound("Flow not found.");

        // ── 2. Node existence ─────────────────────────────────────────────────
        var sourceNode = await _nodes.FirstOrDefaultAsync(
            n => n.Id == request.SourceNodeId && n.FlowId == flowId, ct);
        if (sourceNode is null)
            return FlowResult<EdgeResponse>.Fail("Source node not found in this flow.", 404);

        var targetNode = await _nodes.FirstOrDefaultAsync(
            n => n.Id == request.TargetNodeId && n.FlowId == flowId, ct);
        if (targetNode is null)
            return FlowResult<EdgeResponse>.Fail("Target node not found in this flow.", 404);

        // ── 3. Duplicate check ────────────────────────────────────────────────
        var edgeExists = await _edges.AnyAsync(
            e => e.FlowId == flowId
              && e.SourceNodeId == request.SourceNodeId
              && e.TargetNodeId == request.TargetNodeId, ct);
        if (edgeExists)
            return FlowResult<EdgeResponse>.Fail(
                "An edge already exists between these nodes.", 409);

        // ── 4. Parse conditions JSON (infrastructure concern) ─────────────────
        ConditionGroup? conditions;
        try
        {
            conditions = ConditionsJsonParser.Parse(request.ConditionsJson);
        }
        catch (ArgumentException ex)
        {
            return FlowResult<EdgeResponse>.Fail(ex.Message, 400);
        }

        // ── 5. Domain validation + create ─────────────────────────────────────
        try
        {
            var existingEdges = await _edges.GetByFlowAsync(flowId, ct);
            var flowNodes = await _nodes.GetByFlowAsync(flowId, ct);

            _structure.ValidateEdge(
                sourceNode,
                targetNode,
                conditions,
                existingEdges,
                flowNodes);

            var edge = Edge.Create(
                flowId,
                request.SourceNodeId,
                request.TargetNodeId,
                request.Priority,
                request.ConditionsJson ?? "");

            await _edges.AddAsync(edge, ct);
            await _uow.SaveChangesAsync(ct);

            return FlowResult<EdgeResponse>.Ok(ToResponse(edge));
        }
        catch (DomainException ex)
        {
            return FlowResult<EdgeResponse>.Fail(ex.Message, 422);
        }
        catch (ArgumentException ex)
        {
            return FlowResult<EdgeResponse>.Fail(ex.Message, 400);
        }
    }

    private static EdgeResponse ToResponse(Edge edge) =>
        new(edge.Id,
            edge.FlowId,
            edge.SourceNodeId,
            edge.TargetNodeId,
            edge.Priority,
            edge.ConditionsJson,
            edge.CreatedAt);
}
