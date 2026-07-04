using Application.Contracts.Analytics;
using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Domain.Services;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Services.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Implementations
{
    public sealed class FlowStatsQueryService(
     IUserSessionRepository _sessions,
     ISessionOfferRepository _sessionOffers,
     IUserAnswerRepository _userAnswers,
     ILeadRepository _leads,
     ILeadChannelRepository _leadChannels,
     IDateTimeProvider _time) : IFlowStatsQueryService
    {
        public async Task<FlowStatsResponse> BuildAsync(
            Flow flow, FlowStatsQuery query, CancellationToken ct)
        {
            var flowId = query.FlowId;
            var nodeIds = flow.Nodes.Select(n => n.Id).ToList();
            var questionNodeIds = flow.Nodes
                .Where(n => n.Type == NodeType.Question)
                .Select(n => n.Id).ToList();
            var offerNodeIds = flow.Nodes
                .Where(n => n.Type == NodeType.Offer)
                .Select(n => n.Id).ToList();

            // ── All queries sequential (single DbContext scope) ───────────────
            var sessionStats = await _sessions.GetSessionStatsByFlowAsync(flowId, ct);
            var dailySeries = await _sessions.GetDailySeriesAsync(flowId, query.From, query.To, ct);
            var pathDistribution = await _sessions.GetPathDistributionAsync(flowId, ct);
            var disqualReasons = await _sessions.GetDisqualificationReasonsAsync(flowId, ct);
            var dropOffMap = await _sessions.GetDropOffCountsByFlowAsync(flowId, nodeIds, ct);
            var scoreDistribution = await _sessions.GetScoreDistributionAsync(flowId, ct);

            var flowOfferStats = await _sessionOffers.GetOfferStatsByFlowAsync(flowId, ct);
            var nodeImpressionMap = await _sessionOffers.GetNodeImpressionsByFlowAsync(flowId, nodeIds, ct);

            var answerMap = await _userAnswers.GetAnswerCountsByNodeIdsAsync(nodeIds, ct);
            var answerDistMap = await _userAnswers.GetAnswerDistributionByNodeIdsAsync(questionNodeIds, ct);
            var topTextMap = await _userAnswers.GetTopTextAnswersByNodeIdsAsync(questionNodeIds, topN: 10, ct);
            var avgAnswerSecsMap = await _userAnswers.GetAvgAnswerSecondsByNodeIdsAsync(questionNodeIds, ct);
            var timeStats = await _userAnswers.GetFlowTimeStatsAsync(flowId, ct);

            var tierDistribution = await _leads.GetTierDistributionAsync(flowId, ct);
            var conversionTiming = await _sessionOffers.GetConversionTimingByFlowAsync(flowId, ct);
            var channels = await _leadChannels.GetByFlowIdAsync(flowId, ct);
            var channelStatsMap = await _leadChannels.GetStatsByFlowAsync(flowId, ct);

            // ── Derive totals used across multiple sections ───────────────────
            var total = sessionStats?.TotalSessions ?? 0;
            var completed = sessionStats?.CompletedSessions ?? 0;
            var qualified = sessionStats?.QualifiedSessions ?? 0;
            var disqualified = sessionStats?.DisqualifiedSessions ?? 0;
            var converted = flowOfferStats?.TotalConversions ?? 0;

            return new FlowStatsResponse(
                FlowId: flowId,
                GeneratedAt: _time.UtcNow,

                Summary: BuildSummary(sessionStats, flowOfferStats),

                Timing: new FlowTiming(
                    Session: ToTimingElement(
                        timeStats.AverageSessionDuration,
                        timeStats.MedianSessionDuration,
                        timeStats.MinSessionDuration,
                        timeStats.MaxSessionDuration),
                    Answer: ToTimingElement(
                        timeStats.AverageAnswerDuration,
                        timeStats.MedianAnswerDuration,
                        timeStats.MinAnswerDuration,
                        timeStats.MaxAnswerDuration)),

                Funnel: BuildFunnel(total, completed, qualified, disqualified, converted),

                DailySeries: dailySeries
                    .Select(d => new DailySeriesEntryDto(
                        d.Date, d.Started, d.Completed, d.Qualified, d.Converted))
                    .ToList()
                    .AsReadOnly(),

                ScoreDistribution: scoreDistribution is null
                    ? null
                    : BuildScoreDistribution(scoreDistribution),

                NodeStats: BuildNodeStats(
                    flow, answerMap, answerDistMap, topTextMap,
                    avgAnswerSecsMap, dropOffMap, nodeImpressionMap),

                DisqualificationBreakdown: BuildDisqualBreakdown(
                    disqualReasons, disqualified),

                PathDistribution: BuildPathDistribution(
                    pathDistribution, flow.Nodes),

                TierDistribution: BuildTierDistribution(tierDistribution),

                ChannelStats: BuildChannelStats(channels, channelStatsMap),

                ConversionTiming: conversionTiming is null
                    ? null
                    : new ConversionTimingDto(
                        MedianSeconds: conversionTiming.MedianSeconds,
                        AvgSeconds: conversionTiming.AvgSeconds,
                        PctWithin24Hours: conversionTiming.PctWithin24Hours,
                        SampleSize: conversionTiming.SampleSize)
            );
        }

        // ── Section builders ─────────────────────────────────────────────────

        private static FlowSummaryStats BuildSummary(
            FlowSessionStats? s, FlowOfferStats? o)
        {
            var total = s?.TotalSessions ?? 0;
            var completed = s?.CompletedSessions ?? 0;
            var qualified = s?.QualifiedSessions ?? 0;
            var disqual = s?.DisqualifiedSessions ?? 0;
            var impressions = o?.TotalImpressions ?? 0;
            var conversions = o?.TotalConversions ?? 0;

            return new FlowSummaryStats(
                TotalSessions: total,
                CompletedSessions: completed,
                QualifiedSessions: qualified,
                DisqualifiedSessions: disqual,
                InProgressSessions: s?.InProgressSessions ?? 0,
                AbandonedSessions: s?.AbandonedSessions ?? 0,
                CompletionRate: Rate(completed, total),
                QualificationRate: Rate(qualified, completed),
                DisqualificationRate: Rate(disqual, completed),
                AbandonRate: Rate(s?.AbandonedSessions ?? 0, total),
                TotalOfferImpressions: impressions,
                TotalOfferConversions: conversions,
                OfferConversionRate: Rate(conversions, impressions),
                LastSessionAt: s?.LastSessionAt);
        }

        private static IReadOnlyList<FunnelStageDto> BuildFunnel(
            int total, int completed, int qualified, int disqualified, int converted)
        {
            (string stage, string label, int count)[] stages =
            [
                ("started",   "Started",   total),
                ("completed", "Completed", completed),
                ("qualified", "Qualified", qualified),
                ("converted", "Converted", converted),
            ];

            return stages.Select((s, i) =>
            {
                var prevCount = i == 0 ? total : stages[i - 1].count;

                // At the "qualified" stage the delta vs "completed" represents disqualification
                // (sessions that completed but were routed to a Redirect node), not abandonment.
                // Report it separately so the frontend can label it correctly.
                int? disqualifiedAtStage = null;
                int drop;
                if (s.stage == "qualified")
                {
                    disqualifiedAtStage = disqualified;
                    drop = 0;
                }
                else
                {
                    drop = i == 0 ? 0 : Math.Max(0, prevCount - s.count);
                }

                return new FunnelStageDto(
                    Stage: s.stage,
                    Label: s.label,
                    Count: s.count,
                    RateFromTotal: Rate(s.count, total),
                    DropFromPrevious: drop,
                    DropRateFromPrevious: i == 0 ? 0d : Rate(drop, prevCount),
                    DisqualifiedAtStage: disqualifiedAtStage,
                    AvgSecondsToReach: null);
            }).ToList().AsReadOnly();
        }

        private static IReadOnlyList<NodeStatsEntryDto> BuildNodeStats(
            Flow flow,
            Dictionary<Guid, int> answerMap,
            Dictionary<Guid, List<AnswerOptionStats>> answerDistMap,
            Dictionary<Guid, List<TopTextAnswerRaw>> topTextMap,
            Dictionary<Guid, int> avgAnswerSecsMap,
            Dictionary<Guid, int> dropOffMap,
            Dictionary<Guid, NodeImpressionStats> nodeImpressionMap)
        {
            return flow.Nodes.Select(n =>
            {
                var answered = answerMap.GetValueOrDefault(n.Id);
                var droppedOff = dropOffMap.GetValueOrDefault(n.Id);
                var reached = answered + droppedOff;
                nodeImpressionMap.TryGetValue(n.Id, out var imp);

                var isChoice = n.AnswerType is AnswerType.SingleChoice or AnswerType.MultipleChoice;
                var isText = n.AnswerType is AnswerType.Text;
                var isQuestion = n.Type == NodeType.Question;

                return new NodeStatsEntryDto(
                    NodeId: n.Id,
                    Type: n.Type.ToString(),
                    Title: n.Title,
                    AttributeKey: n.AttributeKey,
                    AnswerType: n.AnswerType?.ToString(),

                    Reached: reached,
                    Answered: isQuestion ? answered : null,
                    DroppedOff: droppedOff,
                    DropOffRate: Rate(droppedOff, reached),
                    AvgAnswerSeconds: avgAnswerSecsMap.TryGetValue(n.Id, out var secs) ? secs : null,

                    AnswerDistribution: isChoice && answerDistMap.TryGetValue(n.Id, out var dist)
                        ? dist.Select(a => new AnswerDistributionItemDto(
                            a.Value, a.Label, a.Count, a.Share,
                            a.QualificationRate, a.AvgScore))
                          .ToList().AsReadOnly()
                        : null,

                    TopTextAnswers: isText && topTextMap.TryGetValue(n.Id, out var texts)
                        ? texts.Select(t => new TopTextAnswerDto(t.Value, t.Count))
                          .ToList().AsReadOnly()
                        : null,

                    OfferImpressions: imp?.Impressions,
                    OfferConversions: imp?.Conversions,
                    OfferConversionRate: imp is { Impressions: > 0 }
                        ? Rate(imp.Conversions, imp.Impressions) : null);
            }).ToList().AsReadOnly();
        }

        private static ScoreDistributionDto BuildScoreDistribution(ScoreDistributionRaw raw)
            => new(
                Min: raw.Min,
                Max: raw.Max,
                Avg: raw.Avg,
                Median: raw.Median,
                QualificationThreshold: raw.QualificationThreshold,
                Buckets: raw.Buckets
                    .Select(b => new ScoreBucketDto(
                        b.From, b.To, $"{b.From:0} – {b.To:0}", b.Count))
                    .ToList().AsReadOnly());

        private static IReadOnlyList<DisqualificationReasonDto> BuildDisqualBreakdown(
            List<DisqualificationReasonRaw> reasons, int totalDisqualified)
            => reasons
                .OrderByDescending(r => r.Count)
                .Select(r => new DisqualificationReasonDto(
                    r.Reason, r.Count, Rate(r.Count, totalDisqualified)))
                .ToList().AsReadOnly();

        private static IReadOnlyList<TierDistributionEntryDto> BuildTierDistribution(
            List<TierDistributionRaw> raw)
        {
            var total = raw.Sum(r => r.Count);
            return raw
                .OrderByDescending(r => r.Count)
                .Select(r => new TierDistributionEntryDto(r.Tier, r.Count, Rate(r.Count, total)))
                .ToList().AsReadOnly();
        }

        private static IReadOnlyList<ChannelStatsEntryDto> BuildChannelStats(
            List<LeadChannel> channels,
            Dictionary<Guid, LeadChannelStats> statsMap)
        {
            return channels.Select(c =>
            {
                statsMap.TryGetValue(c.Id, out var s);
                var sessions = s?.SessionCount ?? 0;
                var qualified = s?.QualifiedCount ?? 0;

                return new ChannelStatsEntryDto(
                    LeadChannelId: c.Id,
                    Name: c.Name,
                    ShortCode: c.ShortCode,
                    IsArchived: c.IsArchived,
                    Sessions: sessions,
                    Qualified: qualified,
                    Disqualified: s?.DisqualifiedCount ?? 0,
                    QualificationRate: Rate(qualified, sessions));
            }).ToList().AsReadOnly();
        }

        private static IReadOnlyList<PathDistributionEntryDto> BuildPathDistribution(
            List<PathDistributionRaw> paths, IReadOnlyCollection<Node> nodes)
        {
            var lookup = nodes.ToDictionary(n => n.Id,
                n => new NodeMinimalInfoDto(
                    n.Id, n.Type.ToString(), n.AttributeKey,
                    n.ValueKind?.ToString(), n.Title, n.AnswerType?.ToString()));

            return paths.Select(x =>
            {
                var nodeList = x.Path
                    .Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Where(s => Guid.TryParse(s, out _))
                    .Select(s => lookup.GetValueOrDefault(Guid.Parse(s)))
                    .OfType<NodeMinimalInfoDto>()
                    .ToList().AsReadOnly();

                return new PathDistributionEntryDto(
                    Path: x.Path,
                    Nodes: nodeList,
                    TerminalType: x.TerminalNodeType,
                    Count: x.Count,
                    Completed: x.Completed,
                    Qualified: x.Qualified,
                    Abandoned: x.Abandoned,
                    InProgress: x.InProgress,
                    ConversionRate: Rate(x.Converted, x.Count));
            }).ToList().AsReadOnly();
        }

        private static FlowTimingElement? ToTimingElement(
            TimeSpan? avg, TimeSpan? median, TimeSpan? min, TimeSpan? max)
            => avg is null ? null
                : new FlowTimingElement(avg, median, min, max);

        private static double Rate(int num, int den)
            => den > 0 ? Math.Round((double)num / den, 4) : 0d;
    }
}
