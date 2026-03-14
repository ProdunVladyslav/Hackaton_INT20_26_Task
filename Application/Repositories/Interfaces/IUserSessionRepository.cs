using Domain.Model.Survey;
using Domain.Model.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Repositories.Interfaces
{
    public interface IUserSessionRepository : IGenericRepository<UserSession>
    {
        Task<UserSession?> GetWithAnswersAsync(Guid sessionId, CancellationToken ct = default);
    }
}
