using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts.Analytics
{
    public sealed record FlowOfferStats(
        int TotalImpressions, 
        int TotalConversions
    );
}
