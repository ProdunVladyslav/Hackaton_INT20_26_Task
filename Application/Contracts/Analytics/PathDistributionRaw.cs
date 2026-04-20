using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts.Analytics
{
    public sealed record PathDistributionRaw(
        string Path,
        int Count,
        int Completed,
        int Abandoned,
        int InProgress
    );
}
