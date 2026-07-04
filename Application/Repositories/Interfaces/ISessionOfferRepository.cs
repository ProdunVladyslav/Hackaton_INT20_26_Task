using Application.Contracts.Analytics;
using Domain.Model.User;

namespace Application.Repositories.Interfaces
{
    public interface ISessionOfferRepository : IGenericRepository<SessionOffer>
    {
        Task<List<SessionOffer>> GetBySessionAsync(Guid sessionId, CancellationToken ct = default);
        Task<SessionOffer?> GetBySessionAndOfferAsync(Guid sessionId, Guid offerId, CancellationToken ct = default);
        Task<List<OfferStatItem>> GetOfferStatsByOwnerAsync(Guid userProfileId, CancellationToken ct = default);
        Task<FlowOfferStats?> GetOfferStatsByFlowAsync(Guid flowId, CancellationToken ct = default);
        Task<Dictionary<Guid, NodeImpressionStats>> GetNodeImpressionsByFlowAsync(Guid flowId, List<Guid> nodeIds, CancellationToken ct = default);
        Task<Dictionary<Guid, FlowOfferStats>> GetOfferStatsByFlowsAsync(CancellationToken ct = default);
        Task<ConversionTimingRaw?> GetConversionTimingByFlowAsync(Guid flowId, CancellationToken ct = default);
    }
}
