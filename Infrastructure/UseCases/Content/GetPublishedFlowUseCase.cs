using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.UseCases.Flows;

namespace Infrastructure.UseCases.Content;

public sealed class GetPublishedFlowUseCase(IFlowRepository _flowRepository)
{
    public async Task<FlowResult<FlowDetailResponse>> ExecuteAsync(CancellationToken ct = default)
    {
        var flow = await _flowRepository.GetFirstPublishedWithDagAsync(ct);
        if (flow is null)
            return FlowResult<FlowDetailResponse>.NotFound("No published flow available.");

        var response = FlowMapper.ToDetail(flow);
        return FlowResult<FlowDetailResponse>.Ok(response);
    }
}
