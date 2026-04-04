using Domain.Model.Survey;
using Domain.Model.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Repositories.Interfaces
{
    public interface IUserAnswerRepository : IGenericRepository<UserAnswer>
    {
        Task<List<UserAnswer>> GetBySessionOrderedAsync(Guid sessionId, CancellationToken ct = default);
        Task<DateTime?> GetLastAnsweredAtAsync(Guid sessionId, CancellationToken ct = default);
        Task<SessionTimeStats> GetTimeStatsAsync(Guid sessionId, CancellationToken ct = default);
        Task<FlowTimeStats> GetFlowTimeStatsAsync(Guid flowId, CancellationToken ct = default);
    }
}
