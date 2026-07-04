using System;

namespace Infrastructure.Contracts.LeadChannels.Responses
{
    public sealed record LeadChannelResponse(
        Guid Id,
        Guid FlowId,
        string Name,
        string ShortCode,
        bool IsArchived,
        DateTime CreatedAt,
        int SessionCount,
        int QualifiedCount,
        int DisqualifiedCount,
        double QualificationRate
    );

    public sealed record LeadChannelResolveResponse(
        Guid FlowId,
        Guid LeadChannelId
    );
}
