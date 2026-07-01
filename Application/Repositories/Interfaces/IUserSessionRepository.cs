using Application.Contracts.Analytics;
using Domain.Model.User;

namespace Application.Repositories.Interfaces
{
    public interface IUserSessionRepository : IGenericRepository<UserSession>
    {
        Task<UserSession?> GetWithAnswersAsync(Guid sessionId, CancellationToken ct = default);
        Task<int> CountByOwnerAsync(Guid userProfileId, CancellationToken ct = default);
        Task<List<DropOffItem>> GetDropOffsByOwnerAsync(Guid userProfileId, CancellationToken ct = default);
        Task<SessionStatsResponse> GetStatsByOwnerAsync(Guid userProfileId, CancellationToken ct = default);
        Task<FlowSessionStats?> GetSessionStatsByFlowAsync(Guid flowId, CancellationToken ct = default);
        Task<Dictionary<Guid, int>> GetDropOffCountsByFlowAsync(Guid flowId, List<Guid> nodeIds, CancellationToken ct = default);
        Task<List<PathDistributionRaw>> GetPathDistributionAsync(Guid flowId, CancellationToken ct = default);
        Task<Dictionary<Guid, FlowSessionStats>> GetSessionStatsByFlowsAsync(CancellationToken ct = default);
        Task<Dictionary<Guid, DurationStats>> GetSessionDurationStatsByFlowsAsync(CancellationToken ct = default);
        Task<List<UserSession>> GetByCurrentNodeIdAsync(Guid nodeId, CancellationToken ct = default);

        Task<List<DailySessionStats>> GetDailySeriesAsync(
            Guid flowId, DateOnly from, DateOnly to, CancellationToken ct = default);

        Task<List<DisqualificationReasonRaw>> GetDisqualificationReasonsAsync(
            Guid flowId, CancellationToken ct = default);

        Task<ScoreDistributionRaw?> GetScoreDistributionAsync(
            Guid flowId, CancellationToken ct = default);
    }
}
