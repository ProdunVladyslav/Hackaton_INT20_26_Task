using Application;
using Application.Contracts.Analytics;
using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Domain.Model.User;
using Infrastructure.Contracts.Flows.Responses;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.UseCases.Flows;

/// <summary>
/// Use case: return a lightweight list of all flows enriched with per-flow admin statistics.
/// Ordered newest-first. Used by the admin dashboard to populate the flow list.
///
/// Stats are fetched with four sequential GROUP BY queries (one per concern) so the
/// main flow list query is never bloated with joins, and each stats query stays
/// independently translatable by EF Core.
///
/// NOTE: EF Core's DbContext is not thread-safe — queries must be awaited one at a time.
/// Do NOT convert these to Task.WhenAll; doing so causes a ConcurrencyDetector exception.
/// </summary>
public sealed class ListFlowsUseCase(
    IFlowRepository _flows,
    INodeRepository _nodes,
    IEdgeRepository _edges,
    IUserSessionRepository _sessions,
    ISessionOfferRepository _sessionOffers,
    IUserAnswerRepository _userAnswers,
    IUserProfileRepository _userProfiles)
{
    public async Task<FlowResult<List<FlowSummaryResponse>>> ExecuteAsync(
        Guid applicationUserId,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(p => p.ApplicationUserId == applicationUserId, ct);
        if (profile == null)
            return FlowResult<List<FlowSummaryResponse>>.NotFound("User profile not found.");

        var flows = await _flows.GetAllOrderedAsync(profile.Id, ct);
        var nodeMap = await _nodes.GetNodeCountsByFlowsAsync(ct);
        var edgeMap = await _edges.GetEdgeCountsByFlowsAsync(ct);
        var sessionMap = await _sessions.GetSessionStatsByFlowsAsync(ct);
        var offerMap = await _sessionOffers.GetOfferStatsByFlowsAsync(ct);
        var sessionDurationMap = await _sessions.GetSessionDurationStatsByFlowsAsync(ct);
        var answerDurationMap = await _userAnswers.GetAnswerDurationStatsByFlowsAsync(ct);

        var summaries = flows.Select(flow =>
        {
            nodeMap.TryGetValue(flow.Id, out var n);
            edgeMap.TryGetValue(flow.Id, out var e);
            sessionMap.TryGetValue(flow.Id, out var s);
            offerMap.TryGetValue(flow.Id, out var o);
            sessionDurationMap.TryGetValue(flow.Id, out var sd);
            answerDurationMap.TryGetValue(flow.Id, out var ad);

            var stats = new FlowAdminStats(
                NodeCount: n?.NodeCount ?? 0,
                EdgeCount: e,
                QuestionCount: n?.QuestionCount ?? 0,
                OfferNodeCount: n?.OfferCount ?? 0,
                InfoPageCount: n?.InfoPageCount ?? 0,

                TotalSessions: s?.TotalSessions ?? 0,
                CompletedSessions: s?.CompletedSessions ?? 0,
                AbandonedSessions: s?.AbandonedSessions ?? 0,
                InProgressSessions: s?.InProgressSessions ?? 0,
                CompletionRate: s is { TotalSessions: > 0 }
                    ? Math.Round((double)s.CompletedSessions / s.TotalSessions * 100, 2) : 0d,
                AbandonRate: s is { TotalSessions: > 0 }
                    ? Math.Round((double)s.AbandonedSessions / s.TotalSessions * 100, 2) : 0d,
                LastSessionAt: s?.LastSessionAt,

                QualifiedSessions: s?.QualifiedSessions ?? 0,
                DisqualifiedSessions: s?.DisqualifiedSessions ?? 0,
                QualificationRate: s is { TotalSessions: > 0 }
                    ? Math.Round((double)s.QualifiedSessions / s.TotalSessions * 100, 2) : 0d,

                TotalOfferImpressions: o?.TotalImpressions ?? 0,
                TotalOfferConversions: o?.TotalConversions ?? 0,
                OfferConversionRate: o is { TotalImpressions: > 0 }
                    ? Math.Round((double)o.TotalConversions / o.TotalImpressions * 100, 2) : 0d,

                AvgSessionDuration: sd?.Avg ?? TimeSpan.Zero,
                MinSessionDuration: sd?.Min ?? TimeSpan.Zero,
                MaxSessionDuration: sd?.Max ?? TimeSpan.Zero,
                MedianSessionDuration: sd?.Median ?? TimeSpan.Zero,

                AvgAnswerDuration: ad?.Avg ?? TimeSpan.Zero,
                MinAnswerDuration: ad?.Min ?? TimeSpan.Zero,
                MaxAnswerDuration: ad?.Max ?? TimeSpan.Zero,
                MedianAnswerDuration: ad?.Median ?? TimeSpan.Zero
            );

            return FlowMapper.ToSummary(flow, stats);
        }).ToList();

        return FlowResult<List<FlowSummaryResponse>>.Ok(summaries);
    }
}