using Application;
using Application.Repositories.Implementations;
using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Domain.Model.User;
using Infrastructure.Contracts.Flows.Responses;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.UseCases.Flows;

/// <summary>
/// Use case: load a single flow with its full DAG (nodes + options + edges)
/// enriched with flow-level and per-node admin analytics.
///
/// Queries executed (all sequential — DbContext is not thread-safe):
///   1. Flow DAG          — nodes, options, edges via repository
///   2. NodeOffers        — offer links for all nodes in one batch
///   3. Session stats     — totals + status breakdown scoped to this flow
///   4. Flow offer stats  — total impressions + conversions scoped to this flow
///   5. Answer counts     — UserAnswers grouped by NodeId (Question nodes)
///   6. Drop-off counts   — UserSessions grouped by CurrentNodeId (all node types)
///   7. Node offer stats  — impressions + conversions per node (Offer nodes)
/// </summary>
public sealed class GetFlowUseCase
{
    private readonly IFlowRepository _flows;
    private readonly IUserAnswerRepository _userAnswerRepository;
    private readonly AppDbContext    _db;

    public GetFlowUseCase(IFlowRepository flows, AppDbContext db, IUserAnswerRepository userAnswerRepository)
    {
        _flows = flows;
        _db    = db;
        _userAnswerRepository = userAnswerRepository;
    }

    public async Task<FlowResult<FlowDetailResponse>> ExecuteAsync(
        Guid flowId,
        CancellationToken ct = default)
    {
        // ── 1. Load DAG ───────────────────────────────────────────────────────
        var flow = await _flows.GetFlowWithDagAsync(flowId, ct);

        if (flow is null)
            return FlowResult<FlowDetailResponse>.NotFound($"Flow {flowId} not found.");

        var nodeIds = flow.Nodes.Select(n => n.Id).ToList();

        // ── 2. NodeOffers ─────────────────────────────────────────────────────
        var nodeOffersByNode = (await _db.NodeOffers
            .Where(no => nodeIds.Contains(no.NodeId))
            .Include(no => no.Offer)
            .ToListAsync(ct))
            .GroupBy(no => no.NodeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // ── 3. Flow-level session stats ───────────────────────────────────────
        var sessionStats = await _db.UserSessions
            .Where(s => s.FlowId == flowId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalSessions      = g.Count(),
                CompletedSessions  = g.Count(s => s.Status == SessionStatus.Completed),
                AbandonedSessions  = g.Count(s => s.Status == SessionStatus.Abandoned),
                InProgressSessions = g.Count(s => s.Status == SessionStatus.InProgress),
                LastSessionAt      = (DateTime?)g.Max(s => s.StartedAt),
            })
            .FirstOrDefaultAsync(ct);

        // ── 4. Flow-level offer stats ─────────────────────────────────────────
        var flowOfferStats = await _db.SessionOffers
            .Join(_db.UserSessions,
                so => so.SessionId,
                s  => s.Id,
                (so, s) => new { so.Converted, s.FlowId })
            .Where(x => x.FlowId == flowId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalImpressions = g.Count(),
                TotalConversions = g.Count(x => x.Converted),
            })
            .FirstOrDefaultAsync(ct);

        // ── 5. Answer counts per node ─────────────────────────────────────────
        var answerMap = (await _db.UserAnswers
            .Where(a => nodeIds.Contains(a.NodeId))
            .GroupBy(a => a.NodeId)
            .Select(g => new { NodeId = g.Key, Count = g.Count() })
            .ToListAsync(ct))
            .ToDictionary(x => x.NodeId, x => x.Count);

        // ── 6. Drop-off counts per node ───────────────────────────────────────
        // Counts sessions (any status) whose last known position is this node.
        var dropOffMap = (await _db.UserSessions
            .Where(s => s.FlowId == flowId && nodeIds.Contains(s.CurrentNodeId))
            .GroupBy(s => s.CurrentNodeId)
            .Select(g => new { NodeId = g.Key, Count = g.Count() })
            .ToListAsync(ct))
            .ToDictionary(x => x.NodeId, x => x.Count);

        // ── 7. Offer impressions + conversions per node ───────────────────────
        // Joins NodeOffers → SessionOffers → UserSessions to scope by flow.
        var nodeImpressionMap = (await (
            from no in _db.NodeOffers
            where nodeIds.Contains(no.NodeId)
            join so in _db.SessionOffers on no.OfferId equals so.OfferId
            join s  in _db.UserSessions  on so.SessionId equals s.Id
            where s.FlowId == flowId
            group new { so.Converted } by no.NodeId into g
            select new
            {
                NodeId      = g.Key,
                Impressions = g.Count(),
                Conversions = g.Count(x => x.Converted),
            }
        ).ToListAsync(ct))
        .ToDictionary(x => x.NodeId);

        // ── 8. User path distribution ─────────────────────────────────────────────
        // Groups sessions by their full node-traversal path and counts occurrences.
        var pathDistribution = await _db.UserSessions
            .Where(s => s.FlowId == flowId && s.UserNodePath != null)
            .GroupBy(s => s.UserNodePath)
            .Select(g => new
            {
                Path = g.Key!,
                Count = g.Count(),
                Completed = g.Count(s => s.Status == SessionStatus.Completed),
                Abandoned = g.Count(s => s.Status == SessionStatus.Abandoned),
                InProgress = g.Count(s => s.Status == SessionStatus.InProgress),
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);

        var nodeMinimalLookup = flow.Nodes.ToDictionary(
            n => n.Id,
            n => new NodeMinimalInfoDto(
                Id: n.Id,
                Type: n.Type.ToString(),
                AttributeKey: n.AttributeKey,
                ValueKind: n.ValueKind?.ToString(),
                Title: n.Title,
                AnswerType: n.AnswerType?.ToString()
            ));

        var pathEntries = pathDistribution
            .Select(x => new PathDistributionEntryDto(
                Path: x.Path,
                Nodes: x.Path
                    .Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Where(raw => Guid.TryParse(raw, out _))
                    .Select(raw => nodeMinimalLookup.GetValueOrDefault(Guid.Parse(raw)))
                    .OfType<NodeMinimalInfoDto>()   // drops nulls (deleted nodes)
                    .ToList()
                    .AsReadOnly(),
                Count: x.Count,
                Completed: x.Completed,
                Abandoned: x.Abandoned,
                InProgress: x.InProgress
            ))
            .ToList();

        // ── 9. Time stats across all sessions ────────────────────────────────
        var timeStats = await _userAnswerRepository.GetFlowTimeStatsAsync(flowId, ct);

        // ── Assemble FlowAdminStats ───────────────────────────────────────────
        // Node/edge counts come from the already-loaded DAG — no extra query.
        var total = sessionStats?.TotalSessions ?? 0;

        var flowStats = new FlowAdminStats(
            NodeCount             : flow.Nodes.Count,
            EdgeCount             : flow.Edges.Count,
            QuestionCount         : flow.Nodes.Count(n => n.Type == NodeType.Question),
            OfferNodeCount        : flow.Nodes.Count(n => n.Type == NodeType.Offer),
            InfoPageCount         : flow.Nodes.Count(n => n.Type == NodeType.InfoPage),

            TotalSessions         : total,
            CompletedSessions     : sessionStats?.CompletedSessions  ?? 0,
            AbandonedSessions     : sessionStats?.AbandonedSessions  ?? 0,
            InProgressSessions    : sessionStats?.InProgressSessions ?? 0,
            CompletionRate        : total > 0
                ? Math.Round((double)sessionStats!.CompletedSessions / total * 100, 2) : 0d,
            AbandonRate           : total > 0
                ? Math.Round((double)sessionStats!.AbandonedSessions / total * 100, 2) : 0d,
            LastSessionAt         : sessionStats?.LastSessionAt,

            TotalOfferImpressions : flowOfferStats?.TotalImpressions ?? 0,
            TotalOfferConversions : flowOfferStats?.TotalConversions ?? 0,
            OfferConversionRate   : flowOfferStats is { TotalImpressions: > 0 }
                ? Math.Round((double)flowOfferStats.TotalConversions / flowOfferStats.TotalImpressions * 100, 2) : 0d,

            // ── Session duration ──────────────────────────────────────────────
            AvgSessionDuration: timeStats.AverageSessionDuration,
            MedianSessionDuration: timeStats.MedianSessionDuration,
            MinSessionDuration: timeStats.MinSessionDuration,
            MaxSessionDuration: timeStats.MaxSessionDuration,

            // ── Answer timing ─────────────────────────────────────────────────
            AvgAnswerDuration: timeStats.AverageAnswerDuration,
            MedianAnswerDuration: timeStats.MedianAnswerDuration,
            MinAnswerDuration: timeStats.MinAnswerDuration,
            MaxAnswerDuration: timeStats.MaxAnswerDuration
        );

        // ── Build base DTO ────────────────────────────────────────────────────
        var detail = FlowMapper.ToDetail(flow, flowStats);

        // ── Enrich each NodeDto with NodeOffers + NodeStatsDto ────────────────
        var enrichedNodes = detail.Nodes.Select(nodeDto =>
        {
            var offers = nodeOffersByNode.TryGetValue(nodeDto.Id, out var list)
                ? list.Select(no => new NodeOfferDto(
                      no.Id,
                      no.OfferId,
                      no.IsPrimary,
                      new OfferDto(
                          no.Offer.Id,
                          no.Offer.Slug,
                          no.Offer.Name,
                          no.Offer.Description,
                          no.Offer.Duration,
                          no.Offer.DigitalContent,
                          no.Offer.PhysicalWellnessKitName,
                          no.Offer.PhysicalWellnessKitItems,
                          no.Offer.Price,
                          no.Offer.ImageUrl,
                          no.Offer.CtaText,
                          no.Offer.CtaUrl
                      )
                  )).ToList()
                : new List<NodeOfferDto>();

            nodeImpressionMap.TryGetValue(nodeDto.Id, out var imp);

            var nodeStats = new NodeStatsDto(
                AnswerCount: answerMap.GetValueOrDefault(nodeDto.Id),
                DroppedOffCount: dropOffMap.GetValueOrDefault(nodeDto.Id),
                OfferImpressions: imp?.Impressions ?? 0,
                OfferConversions: imp?.Conversions ?? 0,
                OfferConversionRate: imp is { Impressions: > 0 }
                    ? Math.Round((double)imp.Conversions / imp.Impressions * 100, 2) : 0d,

                AvgAnswerDuration: timeStats.AverageAnswerDurationByNode
                    .GetValueOrDefault(nodeDto.Id, TimeSpan.Zero)
            );

            return nodeDto with { NodeOffers = offers, Stats = nodeStats };
        }).ToList();

        var attributeKeys = flow.Nodes
            .Where(n => n.Type == NodeType.Question
                     && !string.IsNullOrWhiteSpace(n.AttributeKey)
                     && n.ValueKind.HasValue)
            .GroupBy(n => n.AttributeKey, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var kind = g.First().ValueKind!.Value;
                var allowedOperators = kind switch
                {
                    ValueKind.Text => new[] { "eq", "neq", "in" },
                    ValueKind.Numeric => new[] { "eq", "neq", "in", "gt", "gte", "lt", "lte", "between" },
                    _ => new[] { "eq", "neq" }
                };
                return new AttributeKeyDto(g.Key, kind.ToString(), allowedOperators);
            })
            .OrderBy(x => x.Key)
            .ToList();

        return FlowResult<FlowDetailResponse>.Ok(detail with
        {
            Nodes = enrichedNodes,
            AttributeKeys = attributeKeys,
            PathDistribution = pathEntries,
        });
    }
}
