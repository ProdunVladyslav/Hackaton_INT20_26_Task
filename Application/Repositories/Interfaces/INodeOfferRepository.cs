using Domain.Model.Survey;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Repositories.Interfaces
{
    public interface INodeOfferRepository : IGenericRepository<NodeOffer>
    {
        Task<List<(NodeOffer Link, Offer Offer)>> GetByNodeIdWithOfferAsync(Guid nodeId, CancellationToken ct = default);
    }
}
