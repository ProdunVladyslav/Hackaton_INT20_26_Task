namespace Infrastructure.Contracts.Analytics.Responses;

public sealed record SessionStatsResponse(
    int TotalSessions,
    int InProgress,
    int Completed,
    int Abandoned,
    double CompletionRate,
    double AbandonRate
);
