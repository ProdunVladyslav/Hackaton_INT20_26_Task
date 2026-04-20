using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Contracts.NodeRedirectLinks.Requests
{
    /// <summary>
    /// All fields are optional — only provided values are applied.
    /// </summary>
    public sealed record UpdateNodeRedirectLinkRequest(
        string? Label,
        string? Url,
        int? DisplayOrder);
}
