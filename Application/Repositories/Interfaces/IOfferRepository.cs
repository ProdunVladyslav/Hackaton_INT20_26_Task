using Domain.Model.Survey;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Repositories.Interfaces
{
    public interface IOfferRepository : IGenericRepository<Offer>
    {
        Task<List<Offer>> GetAllOrderedAsync(CancellationToken ct = default);
        Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken ct = default);
        Task<List<Offer>> GetAllOrderedByOwnerAsync(Guid ownerProfileId, CancellationToken ct = default);
    }
}
