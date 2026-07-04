using Application.Contracts.Analytics;
using Domain.Model.Survey;

namespace Application.Repositories.Interfaces
{
    public sealed record LeadChannelStats(int SessionCount, int QualifiedCount, int DisqualifiedCount);

    public interface ILeadChannelRepository : IGenericRepository<LeadChannel>
    {
        Task<LeadChannel?> GetByShortCodeAsync(string shortCode, CancellationToken ct = default);
        Task<bool> ExistsByShortCodeAsync(string shortCode, CancellationToken ct = default);
        Task<List<LeadChannel>> GetByFlowIdAsync(Guid flowId, CancellationToken ct = default);
        Task<Dictionary<Guid, LeadChannelStats>> GetStatsByFlowAsync(Guid flowId, CancellationToken ct = default);
        Task<List<GlobalChannelStatItem>> GetStatsByOwnerAsync(Guid userProfileId, CancellationToken ct = default);
    }
}
