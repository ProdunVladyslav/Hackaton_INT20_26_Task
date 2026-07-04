namespace Application.Contracts.Analytics;

/// <summary>
/// Per-flow statistics embedded inside <see cref="FlowSummaryResponse"/> for admin dashboards.
/// </summary>
public sealed record FlowAdminStats(
    // ── Graph structure ───────────────────────────────────────────────────────
    /// <summary>Total number of nodes in the flow.</summary>
    int NodeCount,
    /// <summary>Total number of directed edges (transitions) in the flow.</summary>
    int EdgeCount,
    /// <summary>How many nodes collect a user answer (type = Question).</summary>
    int QuestionCount,
    /// <summary>How many nodes present offers (type = Offer).</summary>
    int OfferNodeCount,
    /// <summary>How many nodes are informational only (type = InfoPage).</summary>
    int InfoPageCount,

    // ── Session activity ──────────────────────────────────────────────────────
    /// <summary>Total user sessions ever started on this flow.</summary>
    int TotalSessions,
    /// <summary>Sessions that reached the terminal node.</summary>
    int CompletedSessions,
    /// <summary>Sessions explicitly abandoned by the user.</summary>
    int AbandonedSessions,
    /// <summary>Sessions currently in progress.</summary>
    int InProgressSessions,
    /// <summary>Percentage of sessions that completed (0–100, 2 dp).</summary>
    double CompletionRate,
    /// <summary>Percentage of sessions that were abandoned (0–100, 2 dp).</summary>
    double AbandonRate,
    /// <summary>When the most recent session was started; null if no sessions yet.</summary>
    DateTime? LastSessionAt,

    // ── Lead qualification ────────────────────────────────────────────────────
    /// <summary>Sessions that reached an Offer terminal node.</summary>
    int QualifiedSessions,
    /// <summary>Sessions that reached a Redirect (disqualifying) terminal node.</summary>
    int DisqualifiedSessions,
    /// <summary>Percentage of sessions that qualified (0–100, 2 dp).</summary>
    double QualificationRate,

    // ── Offer performance ─────────────────────────────────────────────────────
    /// <summary>Total number of times any offer was presented across all sessions.</summary>
    int TotalOfferImpressions,
    /// <summary>Total number of offer conversions (user clicked CTA).</summary>
    int TotalOfferConversions,
    /// <summary>Overall offer conversion rate for this flow (0–100, 2 dp).</summary>
    double OfferConversionRate,

    // ── Session duration (completed sessions only) ────────────────────────────
    /// <summary>Mean time from session start to completion.</summary>
    TimeSpan AvgSessionDuration,
    /// <summary>Median time from session start to completion — robust against outliers.</summary>
    TimeSpan MedianSessionDuration,
    /// <summary>Shortest completed session duration.</summary>
    TimeSpan MinSessionDuration,
    /// <summary>Longest completed session duration.</summary>
    TimeSpan MaxSessionDuration,

    // ── Answer timing (all sessions) ──────────────────────────────────────────
    /// <summary>Mean time a user spent before submitting any single answer.</summary>
    TimeSpan AvgAnswerDuration,
    /// <summary>Median answer duration — robust against outliers.</summary>
    TimeSpan MedianAnswerDuration,
    /// <summary>Fastest single answer recorded across all sessions.</summary>
    TimeSpan MinAnswerDuration,
    /// <summary>Slowest single answer recorded across all sessions.</summary>
    TimeSpan MaxAnswerDuration
);