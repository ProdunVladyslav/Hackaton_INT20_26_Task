using Domain.Model.Survey;

namespace Application.Repositories.Interfaces
{
    public interface IEdgeRepository : IGenericRepository<Edge>
    {
        Task<Edge?> GetByIdWithOwnerCheckAsync(Guid edgeId, Guid userProfileId, CancellationToken ct = default);
        Task<Dictionary<Guid, int>> GetEdgeCountsByFlowsAsync(CancellationToken ct = default);
        Task<List<Edge>> GetByNodeIdAsync(Guid nodeId, CancellationToken ct = default);
        Task<List<Edge>> GetByAttributeKeyAsync(Guid flowId, string attributeKey, CancellationToken ct = default);
        Task<List<Edge>> GetBySourceNodeAsync(Guid sourceNodeId, Guid flowId, CancellationToken ct = default);
        Task<bool> HasOutgoingEdgesAsync(Guid nodeId, Guid flowId, CancellationToken ct = default);
        Task<List<Edge>> GetByFlowAsync(Guid flowId, CancellationToken ct = default);
    }
}
