using Domain.Model.Survey;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Repositories.Interfaces
{
    public interface INodeRedirectLinkRepository : IGenericRepository<NodeRedirectLink>
    {
        /// <summary>
        /// Returns all links for a given NodeRedirect, ordered by DisplayOrder.
        /// </summary>
        Task<List<NodeRedirectLink>> GetLinksByNodeRedirectIdAsync(Guid nodeRedirectId, CancellationToken ct = default);
    }
}
