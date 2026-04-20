using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Contracts.NodeRedirectLinks.Requests
{
    public sealed record ReorderNodeRedirectLinksRequest(
        IReadOnlyList<ReorderNodeRedirectLinkItem> Items);

    public sealed record ReorderNodeRedirectLinkItem(
        Guid LinkId,
        int DisplayOrder);
}
