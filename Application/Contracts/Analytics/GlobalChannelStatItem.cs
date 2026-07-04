namespace Application.Contracts.Analytics
{
    /// <summary>
    /// Per-channel session/qualification breakdown, tagged with the flow it
    /// belongs to — a LeadChannel is always scoped to one flow, so an
    /// account-wide view needs the flow context the per-flow ChannelStatsEntryDto
    /// doesn't carry (mirrors how OfferStatItem tags each offer with its flow).
    /// </summary>
    public sealed record GlobalChannelStatItem(
        Guid LeadChannelId,
        string Name,
        string ShortCode,
        bool IsArchived,
        Guid FlowId,
        string FlowName,
        int Sessions,
        int Qualified,
        int Disqualified,
        double QualificationRate   // 0.0 – 1.0, matches ChannelStatsEntryDto's convention
    );
}
