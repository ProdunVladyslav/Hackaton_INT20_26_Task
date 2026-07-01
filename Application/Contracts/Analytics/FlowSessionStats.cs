using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts.Analytics
{
    public sealed record FlowSessionStats(
        int TotalSessions,
        int CompletedSessions,
        int AbandonedSessions,
        int QualifiedSessions,
        int DisqualifiedSessions,
        int InProgressSessions,
        DateTime? LastSessionAt
    );
}
