using Application;
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
public sealed class ListFlowsUseCase
{
    private readonly IFlowRepository _flows;
    private readonly AppDbContext _db;

    public ListFlowsUseCase(IFlowRepository flows, AppDbContext db)
    {
        _flows = flows;
        _db = db;
    }

    public async Task<FlowResult<List<FlowSummaryResponse>>> ExecuteAsync(
        CancellationToken ct = default)
    {
        var flows = await _flows.GetAllOrderedAsync(ct);

        // 1. Node counts broken down by NodeType per flow
        var nodeMap = (await _db.Nodes
            .GroupBy(n => n.FlowId)
            .Select(g => new
            {
                FlowId = g.Key,
                NodeCount = g.Count(),
                QuestionCount = g.Count(n => n.Type == NodeType.Question),
                OfferCount = g.Count(n => n.Type == NodeType.Offer),
                InfoPageCount = g.Count(n => n.Type == NodeType.InfoPage),
            })
            .ToListAsync(ct))
            .ToDictionary(x => x.FlowId);

        // 2. Edge counts per flow
        var edgeMap = (await _db.Edges
            .GroupBy(e => e.FlowId)
            .Select(g => new { FlowId = g.Key, EdgeCount = g.Count() })
            .ToListAsync(ct))
            .ToDictionary(x => x.FlowId);

        // 3. Session counts, status breakdown, and last-activity timestamp per flow
        var sessionMap = (await _db.UserSessions
            .GroupBy(s => s.FlowId)
            .Select(g => new
            {
                FlowId = g.Key,
                TotalSessions = g.Count(),
                CompletedSessions = g.Count(s => s.Status == SessionStatus.Completed),
                AbandonedSessions = g.Count(s => s.Status == SessionStatus.Abandoned),
                InProgressSessions = g.Count(s => s.Status == SessionStatus.InProgress),
                LastSessionAt = (DateTime?)g.Max(s => s.StartedAt),
            })
            .ToListAsync(ct))
            .ToDictionary(x => x.FlowId);

        // 4. Offer impression and conversion counts per flow
        var offerMap = (await _db.SessionOffers
            .Join(
                _db.UserSessions,
                so => so.SessionId,
                s => s.Id,
                (so, s) => new { so.Converted, s.FlowId })
            .GroupBy(x => x.FlowId)
            .Select(g => new
            {
                FlowId = g.Key,
                TotalImpressions = g.Count(),
                TotalConversions = g.Count(x => x.Converted),
            })
            .ToListAsync(ct))
            .ToDictionary(x => x.FlowId);

        // 5. Session duration stats — pull completed sessions to memory, compute in C#
        //    EF Core / Npgsql cannot translate DateTime.Ticks arithmetic inside GROUP BY.
        var sessionDurationMap = (await _db.UserSessions
            .Where(s => s.Status == SessionStatus.Completed && s.CompletedAt != null)
            .Select(s => new { s.FlowId, s.StartedAt, CompletedAt = s.CompletedAt!.Value })
            .ToListAsync(ct))                              // ← materialise here
            .GroupBy(s => s.FlowId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var durations = g
                        .Select(s => s.CompletedAt - s.StartedAt)
                        .Where(d => d > TimeSpan.Zero)
                        .OrderBy(d => d)
                        .ToList();

                    if (durations.Count == 0)
                        return (Avg: TimeSpan.Zero, Min: TimeSpan.Zero,
                                Max: TimeSpan.Zero, Median: TimeSpan.Zero);

                    var avg = TimeSpan.FromTicks((long)durations.Average(d => d.Ticks));
                    var min = durations.First();
                    var max = durations.Last();
                    var mid = durations.Count / 2;
                    var median = durations.Count % 2 == 0
                        ? TimeSpan.FromTicks((durations[mid - 1].Ticks + durations[mid].Ticks) / 2)
                        : durations[mid];

                    return (Avg: avg, Min: min, Max: max, Median: median);
                });
        // 6. Answer duration stats — materialise first, then filter and compute in C#
        //    Npgsql cannot translate TimeSpan comparisons (UserAnswerDuration > Zero) to SQL.
        var answerDurationMap = (await _db.UserAnswers
            .Join(_db.UserSessions,
                a => a.SessionId,
                s => s.Id,
                (a, s) => new { s.FlowId, a.UserAnswerDuration })
            .Select(x => new { x.FlowId, x.UserAnswerDuration })
            .ToListAsync(ct))                              // ← materialise before any TimeSpan ops
            .Where(x => x.UserAnswerDuration > TimeSpan.Zero)  // ← C# filter, not SQL
            .GroupBy(x => x.FlowId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var durations = g
                        .Select(x => x.UserAnswerDuration)
                        .OrderBy(d => d)
                        .ToList();

                    if (durations.Count == 0)
                        return (Avg: TimeSpan.Zero, Min: TimeSpan.Zero,
                                Max: TimeSpan.Zero, Median: TimeSpan.Zero);

                    var avg = TimeSpan.FromTicks((long)durations.Average(d => d.Ticks));
                    var min = durations.First();
                    var max = durations.Last();
                    var mid = durations.Count / 2;
                    var median = durations.Count % 2 == 0
                        ? TimeSpan.FromTicks((durations[mid - 1].Ticks + durations[mid].Ticks) / 2)
                        : durations[mid];

                    return (Avg: avg, Min: min, Max: max, Median: median);
                });

        // ── Assemble responses ────────────────────────────────────────────────
        var summaries = flows.Select(flow =>
        {
            nodeMap.TryGetValue(flow.Id, out var n);
            edgeMap.TryGetValue(flow.Id, out var e);
            sessionMap.TryGetValue(flow.Id, out var s);
            offerMap.TryGetValue(flow.Id, out var o);
            sessionDurationMap.TryGetValue(flow.Id, out var sd);
            answerDurationMap.TryGetValue(flow.Id, out var ad);

            var stats = new FlowAdminStats(
                // ── Graph structure ───────────────────────────────────────────
                NodeCount: n?.NodeCount ?? 0,
                EdgeCount: e?.EdgeCount ?? 0,
                QuestionCount: n?.QuestionCount ?? 0,
                OfferNodeCount: n?.OfferCount ?? 0,
                InfoPageCount: n?.InfoPageCount ?? 0,

                // ── Session activity ──────────────────────────────────────────
                TotalSessions: s?.TotalSessions ?? 0,
                CompletedSessions: s?.CompletedSessions ?? 0,
                AbandonedSessions: s?.AbandonedSessions ?? 0,
                InProgressSessions: s?.InProgressSessions ?? 0,
                CompletionRate: s is { TotalSessions: > 0 }
                    ? Math.Round((double)s.CompletedSessions / s.TotalSessions * 100, 2) : 0d,
                AbandonRate: s is { TotalSessions: > 0 }
                    ? Math.Round((double)s.AbandonedSessions / s.TotalSessions * 100, 2) : 0d,
                LastSessionAt: s?.LastSessionAt,

                // ── Offer performance ─────────────────────────────────────────
                TotalOfferImpressions: o?.TotalImpressions ?? 0,
                TotalOfferConversions: o?.TotalConversions ?? 0,
                OfferConversionRate: o is { TotalImpressions: > 0 }
                    ? Math.Round((double)o.TotalConversions / o.TotalImpressions * 100, 2) : 0d,

                // ── Session duration ──────────────────────────────────────────
                AvgSessionDuration: sd.Avg,
                MinSessionDuration: sd.Min,
                MaxSessionDuration: sd.Max,
                MedianSessionDuration: sd.Median,

                // ── Answer timing ─────────────────────────────────────────────
                AvgAnswerDuration: ad.Avg,
                MinAnswerDuration: ad.Min,
                MaxAnswerDuration: ad.Max,
                MedianAnswerDuration: ad.Median
            );

            return FlowMapper.ToSummary(flow, stats);
        }).ToList();

        return FlowResult<List<FlowSummaryResponse>>.Ok(summaries);
    }
}