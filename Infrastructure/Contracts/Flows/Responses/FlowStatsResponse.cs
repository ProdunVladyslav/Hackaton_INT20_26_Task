using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Contracts.Flows.Responses
{
    // ── FlowStatsResponse (expanded) ─────────────────────────────────────────────

    /// <summary>
    /// Full analytics payload for the stats dashboard.
    /// Replaces the partial FlowStatsResponse above.
    /// </summary>
    public sealed record FlowStatsResponse(
        Guid FlowId,
        DateTime GeneratedAt,
        FlowSummaryStats Summary,
        FlowTiming Timing,
        IReadOnlyList<FunnelStageDto> Funnel,
        IReadOnlyList<DailySeriesEntryDto> DailySeries,
        ScoreDistributionDto? ScoreDistribution,        // null when flow has no scoring
        IReadOnlyList<NodeStatsEntryDto> NodeStats,
        IReadOnlyList<DisqualificationReasonDto> DisqualificationBreakdown,
        IReadOnlyList<PathDistributionEntryDto> PathDistribution
    );

    // ── Summary ───────────────────────────────────────────────────────────────────

    public sealed record FlowSummaryStats(
        int TotalSessions,
        int CompletedSessions,
        int QualifiedSessions,       // sessions that terminated at an Offer node
        int DisqualifiedSessions,    // sessions that terminated at a Redirect node
        int InProgressSessions,
        int AbandonedSessions,
        double CompletionRate,
        double QualificationRate,    // QualifiedSessions / CompletedSessions
        double DisqualificationRate, // DisqualifiedSessions / CompletedSessions
        double AbandonRate,
        int TotalOfferImpressions,
        int TotalOfferConversions,
        double OfferConversionRate,
        DateTime? LastSessionAt
    );

    // ── Timing ────────────────────────────────────────────────────────────────────

    public sealed record FlowTiming(
        FlowTimingElement? Session,
        FlowTimingElement? Answer
    );

    public sealed record FlowTimingElement(
        TimeSpan? Avg,
        TimeSpan? Median,
        TimeSpan? Min,
        TimeSpan? Max
    );

    // ── Funnel ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// One stage in the qualification funnel.
    /// Stages are always returned in order: started → completed → qualified → converted.
    /// </summary>
    public sealed record FunnelStageDto(
        string Stage,                  // "started" | "completed" | "qualified" | "converted"
        string Label,                  // display-ready: "Started", "Completed", etc.
        int Count,
        double RateFromTotal,          // Count / TotalSessions
        int DropFromPrevious,          // sessions that abandoned/dropped before this stage
        double DropRateFromPrevious,   // DropFromPrevious / previous stage Count
        /// <summary>
        /// Only non-null for the "qualified" stage.
        /// Counts sessions that completed the flow but were routed to a Redirect (disqualified),
        /// which is distinct from dropping off mid-flow.
        /// </summary>
        int? DisqualifiedAtStage,
        int? AvgSecondsToReach         // null for "started"
    );

    // ── Daily series ──────────────────────────────────────────────────────────────

    /// <summary>
    /// One calendar day of activity. Length matches the requested date range (?from=&to=).
    /// Days with zero activity are included so the chart line is continuous.
    /// </summary>
    public sealed record DailySeriesEntryDto(
        DateOnly Date,
        int Started,
        int Completed,
        int Qualified,
        int Converted
    );

    // ── Score distribution ────────────────────────────────────────────────────────

    public sealed record ScoreDistributionDto(
        double Min,
        double Max,
        double Avg,
        double Median,
        /// <summary>
        /// Null unless a score threshold is explicitly configured on the flow.
        /// When present, the frontend draws a threshold line on the histogram.
        /// </summary>
        double? QualificationThreshold,
        IReadOnlyList<ScoreBucketDto> Buckets
    );

    public sealed record ScoreBucketDto(
        double From,
        double To,
        string Label,   // pre-formatted: "40 – 80"
        int Count
    );

    // ── Node stats ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Per-node analytics. Shape varies by node Type:
    /// - Question nodes: AnswerDistribution or TopTextAnswers populated, never both.
    /// - Offer nodes: OfferImpressions / OfferConversions populated.
    /// - Redirect / Info / LeadCapture: only Reached / DroppedOff meaningful.
    /// </summary>
    public sealed record NodeStatsEntryDto(
        Guid NodeId,
        string Type,
        string Title,
        string? AttributeKey,
        string? AnswerType,

        int Reached,
        int? Answered,              // null for non-question nodes
        int DroppedOff,
        double DropOffRate,         // DroppedOff / Reached
        int? AvgAnswerSeconds,      // null for non-question nodes

        // Question — SingleChoice / MultipleChoice
        IReadOnlyList<AnswerDistributionItemDto>? AnswerDistribution,

        // Question — Text
        IReadOnlyList<TopTextAnswerDto>? TopTextAnswers,

        // Offer
        int? OfferImpressions,
        int? OfferConversions,
        double? OfferConversionRate
    );

    /// <summary>
    /// Stats for one discrete answer option.
    /// qualificationRate = share of sessions that picked this option and
    /// eventually reached an Offer node.
    /// </summary>
    public sealed record AnswerDistributionItemDto(
        string Value,
        string Label,
        int Count,
        double Share,              // Count / total answers for this node
        double QualificationRate,  // 0.0 – 1.0
        double? AvgScore           // null when flow has no scoring
    );

    /// <summary>
    /// One pre-aggregated text answer (normalised: trimmed + lowercased server-side).
    /// </summary>
    public sealed record TopTextAnswerDto(
        string Value,
        int Count
    );

    // ── Disqualification breakdown ────────────────────────────────────────────────

    /// <summary>
    /// Aggregated disqualification reasons from all Redirect node sessions.
    /// Sorted by Count descending.
    /// </summary>
    public sealed record DisqualificationReasonDto(
        string Reason,
        int Count,
        double Share    // Count / DisqualifiedSessions
    );

    // ── Path distribution ─────────────────────────────────────────────────────────

    /// <summary>
    /// One unique path taken through the flow.
    /// TerminalType drives Sankey coloring: "Offer" = green tint, "Redirect" = muted red.
    /// </summary>
    public sealed record PathDistributionEntryDto(
        string Path,                                  // "nodeId>nodeId>nodeId"
        IReadOnlyList<NodeMinimalInfoDto> Nodes,
        string? TerminalType,                         // "Offer" | "Redirect" | null (abandoned)
        int Count,
        int Completed,
        int Qualified,
        int Abandoned,
        int InProgress,
        double ConversionRate                         // OfferConversions / Count for this path
    );

    public sealed record NodeMinimalInfoDto(
        Guid Id,
        string Type,
        string? AttributeKey,
        string? ValueKind,
        string Title,
        string? AnswerType
    );

    // ── Leads (unchanged, for reference) ─────────────────────────────────────────

    public sealed record FlowLeadsResponse(
        Guid FlowId,
        IReadOnlyList<FlowLeadDto> Leads
    );

    public sealed record FlowLeadDto(
        Guid SessionId,
        DateTime SubmittedAt,
        string? Tier,
        bool IsCompleted,
        IReadOnlyList<LeadFieldAnswerDto> Fields
    );

    public sealed record LeadFieldAnswerDto(
        string AttributeKey,
        string FieldType,
        string? Value
    );
}
