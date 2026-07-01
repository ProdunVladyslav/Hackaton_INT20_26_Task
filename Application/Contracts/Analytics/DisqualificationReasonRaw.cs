using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts.Analytics
{
    public sealed record DisqualificationReasonRaw(
        string Reason,
        int Count
    );
}
