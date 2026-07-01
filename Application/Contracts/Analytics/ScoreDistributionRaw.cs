using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts.Analytics
{
    public sealed record ScoreDistributionRaw(
        double Min,
        double Max,
        double Avg,
        double Median,
        double? QualificationThreshold,   // null until you add threshold to Flow domain
        List<ScoreBucketRaw> Buckets
    );

    public sealed record ScoreBucketRaw(double From, double To, int Count);
}
