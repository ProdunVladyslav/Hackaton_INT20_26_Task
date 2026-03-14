using Domain.Model.Survey;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Repositories.Interfaces
{
    public interface IOptionRepository : IGenericRepository<Option>
    {
        Task<List<Option>> GetOptionsByNodeIdAsync(Guid nodeId, CancellationToken ct = default);
    }
}
