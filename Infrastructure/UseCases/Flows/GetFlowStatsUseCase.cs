using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Domain.Services;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Services.Interfaces;

namespace Infrastructure.UseCases.Flows;

/// <summary>
/// Use case: load analytics for a single flow — session stats, offer stats,
/// per-node metrics, and path distribution.
/// Does NOT return the DAG structure — use <see cref="GetFlowUseCase"/> for that.
///
/// Queries executed (all sequential — DbContext is not thread-safe):
///   1. Session stats       — totals + status breakdown scoped to this flow
///   2. Flow offer stats    — total impressions + conversions scoped to this flow
///   3. Answer counts       — UserAnswers grouped by NodeId (Question nodes)
///   4. Drop-off counts     — UserSessions grouped by CurrentNodeId (all node types)
///   5. Node offer stats    — impressions + conversions per node (Offer nodes)
///   6. Path distribution   — UserSessions grouped by path taken
///   7. Time stats          — session and answer durations for this flow
/// </summary>
public sealed class GetFlowStatsUseCase(
    IFlowRepository _flows,
    IUserProfileRepository _userProfiles,
    IFlowStatsQueryService _statsQuery,
    IDateTimeProvider _time)
{
    public async Task<FlowResult<FlowStatsResponse>> ExecuteAsync(
        Guid flowId,
        Guid applicationUserId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles
            .FirstOrDefaultAsync(p => p.ApplicationUserId == applicationUserId, ct);
        if (profile is null)
            return FlowResult<FlowStatsResponse>.NotFound("User profile not found.");

        var flow = await _flows.GetFlowWithDagAsync(flowId, profile.Id, ct);
        if (flow is null)
            return FlowResult<FlowStatsResponse>.NotFound($"Flow {flowId} not found.");

        var today = _time.Today;
        var query = new FlowStatsQuery(
            FlowId: flowId,
            From: from ?? today.AddDays(-29),
            To: to ?? today);

        var response = await _statsQuery.BuildAsync(flow, query, ct);
        return FlowResult<FlowStatsResponse>.Ok(response);
    }
}