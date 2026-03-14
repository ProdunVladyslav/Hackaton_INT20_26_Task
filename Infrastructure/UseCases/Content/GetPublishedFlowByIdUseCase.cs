using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.UseCases.Flows;

namespace Infrastructure.UseCases.Content;

public sealed class GetPublishedFlowByIdUseCase
{
    private readonly IFlowRepository _flowRepository;

    public GetPublishedFlowByIdUseCase(IFlowRepository flowRepository)
    {
        _flowRepository = flowRepository;
    }

    public async Task<FlowResult<FlowDetailResponse>> ExecuteAsync(Guid flowId, CancellationToken ct = default)
    {
        var flow = await _flowRepository.GetPublishedFlowWithDagAsync(flowId, ct);
        if (flow is null)
            return FlowResult<FlowDetailResponse>.NotFound("Published flow not found.");

        var response = FlowMapper.ToDetail(flow);
        return FlowResult<FlowDetailResponse>.Ok(response);
    }
}
