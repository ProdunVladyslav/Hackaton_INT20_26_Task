using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts.Analytics
{
    public sealed record AnswerOptionStats(
        string Value,
        string Label,
        int Count,
        double Share,
        double QualificationRate,
        double? AvgScore
    );
}
