using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Flows;

/// <summary>
/// Use case: return a lightweight list of all flows (no DAG data).
/// Ordered newest-first. Used by the admin dashboard to populate the flow list.
/// </summary>
public sealed class ListFlowsUseCase
{
    private readonly IFlowRepository _flows;

    public ListFlowsUseCase(IFlowRepository flows) => _flows = flows;

    public async Task<FlowResult<List<FlowSummaryResponse>>> ExecuteAsync(
        CancellationToken ct = default)
    {
        var flows = await _flows.GetAllOrderedAsync(ct);

        var summaries = flows
            .Select(FlowMapper.ToSummary)
            .ToList();

        return FlowResult<List<FlowSummaryResponse>>.Ok(summaries);
    }
}
