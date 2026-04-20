using Application.Contracts.Analytics;
using Domain.Model.Survey;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Repositories.Interfaces
{
    public interface INodeRepository : IGenericRepository<Node>
    {
        Task<Dictionary<Guid, NodeCountStats>> GetNodeCountsByFlowsAsync(CancellationToken ct = default);
        Task<List<Node>> GetByAttributeKeyAsync(Guid flowId, string attributeKey, Guid excludeNodeId, CancellationToken ct = default);
        Task<Node?> GetWithOptionsAsync(Guid nodeId, CancellationToken ct = default);
        Task<List<Node>> GetByFlowAsync(Guid flowId, CancellationToken ct = default);
    }
}
