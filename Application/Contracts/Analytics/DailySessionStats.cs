using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts.Analytics
{
    public sealed record DailySessionStats(
        DateOnly Date,
        int Started,
        int Completed,
        int Qualified,
        int Converted
    );
}
