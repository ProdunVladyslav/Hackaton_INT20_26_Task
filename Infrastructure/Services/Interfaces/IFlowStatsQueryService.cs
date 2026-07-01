using Domain.Model.Survey;
using Infrastructure.Contracts.Flows.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Interfaces
{
    public interface IFlowStatsQueryService
    {
        Task<FlowStatsResponse> BuildAsync(
            Flow flow,
            FlowStatsQuery query,
            CancellationToken ct = default);
    }

    public sealed record FlowStatsQuery(
        Guid FlowId,
        DateOnly From,
        DateOnly To
    );
}
