using Application;
using Infrastructure.Contracts.Analytics.Responses;
using Infrastructure.Contracts.Flows.Responses;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.UseCases.Analytics;

/// <summary>
/// Use case: Get offer performance statistics.
///
/// Returns per-offer metrics:
/// - Times presented
/// - Times converted
/// - Conversion rate (%)
/// </summary>
public sealed class OfferStatsUseCase
{
    private readonly AppDbContext _db;

    public OfferStatsUseCase(AppDbContext db) => _db = db;

    public async Task<FlowResult<OfferStatsResponse>> ExecuteAsync(CancellationToken ct = default)
    {
        var raw = await _db.SessionOffers
            .Join(_db.Offers, so => so.OfferId, o => o.Id, (so, o) => new { so, o })
            .GroupBy(x => new { x.o.Id, x.o.Name, x.o.Slug })
            .Select(g => new
            {
                g.Key.Id,
                g.Key.Name,
                g.Key.Slug,
                TimesPresented = g.Count(),
                TimesConverted = g.Count(x => x.so.Converted)
            })
            .ToListAsync(ct);

        var items = raw
            .Select(x => new OfferStatItem(
                x.Id,
                x.Name,
                x.Slug,
                x.TimesPresented,
                x.TimesConverted,
                x.TimesPresented > 0
                    ? Math.Round((double)x.TimesConverted / x.TimesPresented * 100, 2)
                    : 0
            ))
            .OrderByDescending(x => x.TimesPresented)
            .ToList();

        return FlowResult<OfferStatsResponse>.Ok(new OfferStatsResponse(items));
    }
}
