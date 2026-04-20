using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Flows.Responses;

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
public sealed class GetFlowUseCase(
    IFlowRepository _flows,
    IUserProfileRepository _userProfiles,
    INodeOfferRepository _nodeOffers,
    IUserSessionRepository _sessions,
    ISessionOfferRepository _sessionOffers,
    IUserAnswerRepository _userAnswers)
{
    public async Task<FlowResult<FlowDetailResponse>> ExecuteAsync(
        Guid flowId,
        Guid applicationUserId,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(p => p.ApplicationUserId == applicationUserId, ct);
        if (profile == null)
            return FlowResult<FlowDetailResponse>.NotFound("User profile not found.");

        var flow = await _flows.GetFlowWithDagAsync(flowId, profile.Id, ct);
        if (flow is null)
            return FlowResult<FlowDetailResponse>.NotFound($"Flow {flowId} not found.");

        var nodeIds = flow.Nodes.Select(n => n.Id).ToList();

        var nodeOffersByNode = await _nodeOffers.GetByNodeIdsWithOffersAsync(nodeIds, ct);
        var sessionStats = await _sessions.GetSessionStatsByFlowAsync(flowId, ct);
        var flowOfferStats = await _sessionOffers.GetOfferStatsByFlowAsync(flowId, ct);
        var answerMap = await _userAnswers.GetAnswerCountsByNodeIdsAsync(nodeIds, ct);
        var dropOffMap = await _sessions.GetDropOffCountsByFlowAsync(flowId, nodeIds, ct);
        var nodeImpressionMap = await _sessionOffers.GetNodeImpressionsByFlowAsync(flowId, nodeIds, ct);
        var pathDistribution = await _sessions.GetPathDistributionAsync(flowId, ct);
        var timeStats = await _userAnswers.GetFlowTimeStatsAsync(flowId, ct);

        // ── Path distribution ─────────────────────────────────────────────────
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
                    .OfType<NodeMinimalInfoDto>()
                    .ToList()
                    .AsReadOnly(),
                Count: x.Count,
                Completed: x.Completed,
                Abandoned: x.Abandoned,
                InProgress: x.InProgress
            ))
            .ToList();

        // ── FlowAdminStats ────────────────────────────────────────────────────
        var total = sessionStats?.TotalSessions ?? 0;

        var flowStats = new FlowAdminStats(
            NodeCount: flow.Nodes.Count,
            EdgeCount: flow.Edges.Count,
            QuestionCount: flow.Nodes.Count(n => n.Type == NodeType.Question),
            OfferNodeCount: flow.Nodes.Count(n => n.Type == NodeType.Offer),
            InfoPageCount: flow.Nodes.Count(n => n.Type == NodeType.InfoPage),

            TotalSessions: total,
            CompletedSessions: sessionStats?.CompletedSessions ?? 0,
            AbandonedSessions: sessionStats?.AbandonedSessions ?? 0,
            InProgressSessions: sessionStats?.InProgressSessions ?? 0,
            CompletionRate: total > 0
                ? Math.Round((double)sessionStats!.CompletedSessions / total * 100, 2) : 0d,
            AbandonRate: total > 0
                ? Math.Round((double)sessionStats!.AbandonedSessions / total * 100, 2) : 0d,
            LastSessionAt: sessionStats?.LastSessionAt,

            TotalOfferImpressions: flowOfferStats?.TotalImpressions ?? 0,
            TotalOfferConversions: flowOfferStats?.TotalConversions ?? 0,
            OfferConversionRate: flowOfferStats is { TotalImpressions: > 0 }
                ? Math.Round((double)flowOfferStats.TotalConversions / flowOfferStats.TotalImpressions * 100, 2) : 0d,

            AvgSessionDuration: timeStats.AverageSessionDuration,
            MedianSessionDuration: timeStats.MedianSessionDuration,
            MinSessionDuration: timeStats.MinSessionDuration,
            MaxSessionDuration: timeStats.MaxSessionDuration,

            AvgAnswerDuration: timeStats.AverageAnswerDuration,
            MedianAnswerDuration: timeStats.MedianAnswerDuration,
            MinAnswerDuration: timeStats.MinAnswerDuration,
            MaxAnswerDuration: timeStats.MaxAnswerDuration
        );

        // ── Base DTO ──────────────────────────────────────────────────────────
        var detail = FlowMapper.ToDetail(flow, flowStats);

        // ── Enrich nodes ──────────────────────────────────────────────────────
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
                          no.Offer.Headline,
                          no.Offer.Body,
                          no.Offer.ImageUrl,
                          no.Offer.CalendarUrl,
                          no.CalendarProvider?.ToString(),
                          no.Tier.ToString(),
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

            // ── Resolve domain node for type-specific data ────────────────────────
            var domainNode = flow.Nodes.First(n => n.Id == nodeDto.Id);

            var redirectDto = domainNode.Redirect is { } r
                ? new NodeRedirectDto(
                    Id: r.Id,
                    RedirectUrl: r.RedirectUrl,
                    AutoRedirectAfterSeconds: r.AutoRedirectAfterSeconds,
                    Tier: r.Tier.ToString(),
                    Links: r.Links.Select(l => new NodeRedirectLinkDto(
                        Id: l.Id,
                        Label: l.Label,
                        Url: l.Url
                    )).ToList().AsReadOnly())
                : null;

            var leadCaptureDto = domainNode.LeadCapture is { } lc
                ? new NodeLeadCaptureDto(
                    Id: lc.Id,
                    IsRequired: lc.IsRequired,
                    Fields: lc.Fields
                        .OrderBy(f => f.DisplayOrder)
                        .Select(f => new NodeLeadCaptureFieldDto(
                            Id: f.Id,
                            FieldType: f.FieldType.ToString(),
                            AttributeKey: f.AttributeKey,
                            IsRequired: f.IsRequired,
                            DisplayOrder: f.DisplayOrder,
                            Placeholder: f.Placeholder
                        )).ToList().AsReadOnly())
                : null;

            return nodeDto with
            {
                NodeOffers = offers,
                Stats = nodeStats,
                Redirect = redirectDto,
                LeadCapture = leadCaptureDto
            };
        }).ToList();

        // ── AttributeKeys ─────────────────────────────────────────────────────
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
