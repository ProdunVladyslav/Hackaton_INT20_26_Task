using Application.Contracts.Analytics;
using Domain.Model.Survey;
using Domain.Model.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Repositories.Interfaces
{
    public interface IUserAnswerRepository : IGenericRepository<UserAnswer>
    {
        Task<List<UserAnswer>> GetBySessionOrderedAsync(Guid sessionId, CancellationToken ct = default);
        Task<DateTime?> GetLastAnsweredAtAsync(Guid sessionId, CancellationToken ct = default);
        Task<SessionTimeStats> GetTimeStatsAsync(Guid sessionId, CancellationToken ct = default);
        Task<FlowTimeStats> GetFlowTimeStatsAsync(Guid flowId, CancellationToken ct = default);
        Task<Dictionary<Guid, int>> GetAnswerCountsByNodeIdsAsync(List<Guid> nodeIds, CancellationToken ct = default);
        Task<Dictionary<Guid, DurationStats>> GetAnswerDurationStatsByFlowsAsync(CancellationToken ct = default);
        Task<List<UserAnswer>> GetBySessionAsync(Guid sessionId, CancellationToken ct = default);
        Task<decimal?> ResolveNumericValueAsync(string storedValue, CancellationToken ct = default);
        Task DeleteByNodeIdAsync(Guid nodeId, CancellationToken ct = default);

        Task<Dictionary<Guid, List<AnswerOptionStats>>> GetAnswerDistributionByNodeIdsAsync(
           List<Guid> nodeIds, CancellationToken ct = default);

        Task<Dictionary<Guid, List<TopTextAnswerRaw>>> GetTopTextAnswersByNodeIdsAsync(
            List<Guid> nodeIds, int topN, CancellationToken ct = default);

        Task<Dictionary<Guid, int>> GetAvgAnswerSecondsByNodeIdsAsync(
            List<Guid> nodeIds, CancellationToken ct = default);
    }
}
