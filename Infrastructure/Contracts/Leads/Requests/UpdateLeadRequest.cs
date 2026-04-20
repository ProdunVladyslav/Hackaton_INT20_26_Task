using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Contracts.Leads.Requests
{
    public sealed record UpdateLeadRequest(
         string? Status = null,
         string? Tier = null,
         string? Notes = null,
         bool UpdateAssignment = false,  // must be true to change AssignedToId
         Guid? AssignedToId = null       // null + UpdateAssignment=true = clear
     );
}
