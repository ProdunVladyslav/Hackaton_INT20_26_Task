using Domain.Model.Survey;
using Domain.Model.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Repositories.Interfaces
{
    public interface ISessionOfferRepository : IGenericRepository<SessionOffer>
    {
        Task<List<SessionOffer>> GetBySessionAsync(Guid sessionId, CancellationToken ct = default);
        Task<SessionOffer?> GetBySessionAndOfferAsync(Guid sessionId, Guid offerId, CancellationToken ct = default);
    }
}
