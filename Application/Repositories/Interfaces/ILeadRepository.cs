using Application.Contracts.Leads;
using Domain.Model.User;

namespace Application.Repositories.Interfaces
{
    public interface ILeadRepository : IGenericRepository<Lead>
    {
        Task<Lead?> GetBySessionIdAsync(Guid sessionId, CancellationToken ct = default);
        Task<List<Lead>> GetByFlowAsync(Guid flowId, CancellationToken ct = default);
        Task<(List<Lead> Items, int TotalCount)> GetByFlowFilteredAsync(
            Guid flowId, ListLeadsRequest request, CancellationToken ct = default);
    }
}
