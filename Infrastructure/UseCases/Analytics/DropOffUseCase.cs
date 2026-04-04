using Application;
using Domain.Model.User;
using Infrastructure.Contracts.Analytics.Responses;
using Infrastructure.Contracts.Flows.Responses;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.UseCases.Analytics;

/// <summary>
/// Use case: Get drop-off analysis.
///
/// Returns for each node where users abandon:
/// - Node ID and title
/// - Count of sessions still on that node
/// - Drop-off rate (% of total sessions)
/// </summary>
public sealed class DropOffUseCase
{
    private readonly AppDbContext _db;

    public DropOffUseCase(AppDbContext db) => _db = db;

    public async Task<FlowResult<DropOffResponse>> ExecuteAsync(CancellationToken ct = default)
    {
        var total = await _db.UserSessions.CountAsync(ct);

        var dropoffs = await _db.UserSessions
            .Where(s => s.Status == SessionStatus.Abandoned)
            .Join(_db.Nodes, s => s.CurrentNodeId, n => n.Id, (s, n) => new { s, n })
            .Join(_db.Flows, x => x.n.FlowId, f => f.Id, (x, f) => new { x.n, f })
            .GroupBy(x => new
            {
                NodeId = x.n.Id,
                NodeTitle = x.n.Title,
                FlowId = x.f.Id,
                FlowTitle = x.f.Name
            })
            .Select(g => new
            {
                g.Key.NodeId,
                g.Key.NodeTitle,
                g.Key.FlowId,
                g.Key.FlowTitle,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);

        var items = dropoffs.Select(d => new DropOffItem(
            d.NodeId,
            d.NodeTitle,
            d.FlowId,
            d.FlowTitle,
            d.Count,
            total > 0 ? Math.Round((double)d.Count / total * 100, 2) : 0
        )).ToList();

        return FlowResult<DropOffResponse>.Ok(new DropOffResponse(items));
    }
}
