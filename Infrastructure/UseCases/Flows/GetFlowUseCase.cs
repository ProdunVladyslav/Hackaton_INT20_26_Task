using Application;
using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Flows.Responses;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.UseCases.Flows;

/// <summary>
/// Use case: load a single flow with its full DAG (nodes + options + edges).
/// Returns the complete graph needed by the admin visual editor.
/// </summary>
public sealed class GetFlowUseCase
{
    private readonly IFlowRepository _flows;
    private readonly AppDbContext    _db;

    public GetFlowUseCase(IFlowRepository flows, AppDbContext db)
    {
        _flows = flows;
        _db    = db;
    }

    public async Task<FlowResult<FlowDetailResponse>> ExecuteAsync(
        Guid flowId,
        CancellationToken ct = default)
    {
        var flow = await _flows.GetFlowWithDagAsync(flowId, ct);

        if (flow is null)
            return FlowResult<FlowDetailResponse>.NotFound($"Flow {flowId} not found.");

        // Load NodeOffers for all nodes in this flow in a single query,
        // then stitch them into the DTO manually (they live outside the aggregate).
        var nodeIds = flow.Nodes.Select(n => n.Id).ToList();

        var nodeOffers = await _db.NodeOffers
            .Where(no => nodeIds.Contains(no.NodeId))
            .ToListAsync(ct);

        var nodeOffersByNode = nodeOffers
            .GroupBy(no => no.NodeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var detail = FlowMapper.ToDetail(flow);

        // Enrich each NodeDto with its NodeOffers
        var enrichedNodes = detail.Nodes.Select(nodeDto =>
        {
            var offers = nodeOffersByNode.TryGetValue(nodeDto.Id, out var list)
                ? list.Select(no => new NodeOfferDto(no.Id, no.OfferId, no.IsPrimary)).ToList()
                : new List<NodeOfferDto>();

            return nodeDto with { NodeOffers = offers };
        }).ToList();

        return FlowResult<FlowDetailResponse>.Ok(detail with { Nodes = enrichedNodes });
    }
}
