using Domain.Model.Survey;

namespace Application.Repositories.Interfaces
{
    public interface IFlowRepository : IGenericRepository<Flow>
    {
        /// <summary>
        /// Loads a single flow together with its complete DAG:
        /// nodes (with options and node-offer links) and edges.
        /// Used by the admin editor to render the full canvas in one round-trip.
        /// </summary>
        Task<Flow?> GetFlowWithDagAsync(Guid flowId, CancellationToken ct = default);

        /// <summary>
        /// Returns all flows ordered by creation date (newest first),
        /// without loading nodes/edges (for summary list endpoints).
        /// </summary>
        Task<List<Flow>> GetAllOrderedAsync(Guid ownerProfileId, CancellationToken ct = default);

        /// <summary>
        /// Returns the first published flow (newest) with full DAG.
        /// Used by public content delivery endpoints.
        /// </summary>
        Task<Flow?> GetFirstPublishedWithDagAsync(CancellationToken ct = default);

        /// <summary>
        /// Returns a specific published flow by ID with full DAG.
        /// Used by public content delivery endpoints.
        /// </summary>
        Task<Flow?> GetPublishedFlowWithDagAsync(Guid flowId, CancellationToken ct = default);

        Task<Flow?> GetFlowWithDagAsync(Guid flowId, Guid ownerProfileId, CancellationToken ct = default);
    }
}
