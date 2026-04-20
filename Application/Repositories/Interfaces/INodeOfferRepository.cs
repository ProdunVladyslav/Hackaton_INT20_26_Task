using Application.Contracts.Quiz;
using Domain.Model.Survey;

namespace Application.Repositories.Interfaces
{
    public interface INodeOfferRepository : IGenericRepository<NodeOffer>
    {
        Task<List<(NodeOffer Link, Offer Offer)>> GetByNodeIdWithOfferAsync(Guid nodeId, CancellationToken ct = default);
        Task<Dictionary<Guid, List<NodeOffer>>> GetByNodeIdsWithOffersAsync(List<Guid> nodeIds, CancellationToken ct = default);
        Task DeleteByNodeIdAsync(Guid nodeId, CancellationToken ct = default);
        Task<List<NodeOfferWithOffer>> GetByNodeIdWithOfferForQuizAsync(Guid nodeId, CancellationToken ct = default);
        Task<List<NodeOffer>> GetByNodeIdAsync(Guid nodeId, CancellationToken ct = default);
    }
}
