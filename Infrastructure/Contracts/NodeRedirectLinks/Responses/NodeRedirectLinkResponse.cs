using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Contracts.NodeRedirectLinks.Responses
{
    public sealed record NodeRedirectLinkResponse(
        Guid Id,
        Guid NodeRedirectId,
        string Label,
        string Url,
        int DisplayOrder);
}
