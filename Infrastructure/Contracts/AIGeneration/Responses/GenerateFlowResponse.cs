using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Contracts.AIGeneration.Responses
{
    public sealed record GenerateFlowResponse(Guid FlowId);

    public sealed record GenerateFlowStatusResponse(
        Guid JobId,
        string Status,   // Pending | Running | Done | Failed
        Guid? FlowId,
        string? Error);

    public sealed record GenerateFlowJobAcceptedResponse(Guid JobId);
}
