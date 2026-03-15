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
    private readonly AppDbContext    _db;

    public ListFlowsUseCase(IFlowRepository flows, AppDbContext db)
    {
        _flows = flows;
        _db    = db;
    }

    public async Task<FlowResult<List<FlowSummaryResponse>>> ExecuteAsync(
        CancellationToken ct = default)
    {
        var flows = await _flows.GetAllOrderedAsync(ct);

        // ── Run four stats queries sequentially (DbContext is not thread-safe) ─

        // 1. Node counts broken down by NodeType per flow
        var nodeMap = (await _db.Nodes
            .GroupBy(n => n.FlowId)
            .Select(g => new
            {
                FlowId        = g.Key,
                NodeCount     = g.Count(),
                QuestionCount = g.Count(n => n.Type == NodeType.Question),
                OfferCount    = g.Count(n => n.Type == NodeType.Offer),
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
                FlowId             = g.Key,
                TotalSessions      = g.Count(),
                CompletedSessions  = g.Count(s => s.Status == SessionStatus.Completed),
                AbandonedSessions  = g.Count(s => s.Status == SessionStatus.Abandoned),
                InProgressSessions = g.Count(s => s.Status == SessionStatus.InProgress),
                LastSessionAt      = (DateTime?)g.Max(s => s.StartedAt),
            })
            .ToListAsync(ct))
            .ToDictionary(x => x.FlowId);

        // 4. Offer impression and conversion counts per flow
        //    SessionOffer has no direct FlowId, so we join through UserSession.
        var offerMap = (await _db.SessionOffers
            .Join(
                _db.UserSessions,
                so => so.SessionId,
                s  => s.Id,
                (so, s) => new { so.Converted, s.FlowId })
            .GroupBy(x => x.FlowId)
            .Select(g => new
            {
                FlowId           = g.Key,
                TotalImpressions = g.Count(),
                TotalConversions = g.Count(x => x.Converted),
            })
            .ToListAsync(ct))
            .ToDictionary(x => x.FlowId);

        // ── Assemble responses ────────────────────────────────────────────────
        var summaries = flows.Select(flow =>
        {
            nodeMap.TryGetValue(flow.Id,    out var n);
            edgeMap.TryGetValue(flow.Id,    out var e);
            sessionMap.TryGetValue(flow.Id, out var s);
            offerMap.TryGetValue(flow.Id,   out var o);

            var stats = new FlowAdminStats(
                NodeCount     : n?.NodeCount     ?? 0,
                EdgeCount     : e?.EdgeCount     ?? 0,
                QuestionCount : n?.QuestionCount ?? 0,
                OfferNodeCount: n?.OfferCount    ?? 0,
                InfoPageCount : n?.InfoPageCount ?? 0,

                TotalSessions         : s?.TotalSessions      ?? 0,
                CompletedSessions     : s?.CompletedSessions  ?? 0,
                AbandonedSessions     : s?.AbandonedSessions  ?? 0,
                InProgressSessions    : s?.InProgressSessions ?? 0,
                CompletionRate        : s is { TotalSessions: > 0 }
                    ? Math.Round((double)s.CompletedSessions  / s.TotalSessions * 100, 2)
                    : 0d,
                AbandonRate           : s is { TotalSessions: > 0 }
                    ? Math.Round((double)s.AbandonedSessions  / s.TotalSessions * 100, 2)
                    : 0d,
                LastSessionAt         : s?.LastSessionAt,

                TotalOfferImpressions : o?.TotalImpressions ?? 0,
                TotalOfferConversions : o?.TotalConversions ?? 0,
                OfferConversionRate   : o is { TotalImpressions: > 0 }
                    ? Math.Round((double)o.TotalConversions / o.TotalImpressions * 100, 2)
                    : 0d
            );

            return FlowMapper.ToSummary(flow, stats);
        }).ToList();

        return FlowResult<List<FlowSummaryResponse>>.Ok(summaries);
    }
}
