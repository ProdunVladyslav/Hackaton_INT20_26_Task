using Application.Contracts.Leads;
using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Domain.Model.User;
using Microsoft.EntityFrameworkCore;

namespace Application.Repositories.Implementations
{
    public class LeadRepository(AppDbContext context) : GenericRepository<Lead>(context), ILeadRepository
    {
        public async Task<Lead?> GetBySessionIdAsync(Guid sessionId, CancellationToken ct = default)
            => await _context.Leads
                .FirstOrDefaultAsync(l => l.SessionId == sessionId, ct);

        public async Task<List<Lead>> GetByFlowAsync(Guid flowId, CancellationToken ct = default)
            => await _context.Leads
                .Where(l => l.FlowId == flowId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync(ct);

        public async Task<(List<Lead> Items, int TotalCount)> GetByFlowFilteredAsync(
            Guid flowId, ListLeadsRequest request, CancellationToken ct = default)
        {
            var query = _context.Leads.Where(l => l.FlowId == flowId);

            // ── Filters ───────────────────────────────────────────────────────────
            if (request.Tier is not null
                && Enum.TryParse<QualificationTier>(request.Tier, ignoreCase: true, out var tier))
                query = query.Where(l => l.Tier == tier);

            if (request.Status is not null
                && Enum.TryParse<LeadStatus>(request.Status, ignoreCase: true, out var status))
                query = query.Where(l => l.Status == status);

            if (request.From.HasValue)
            {
                var fromDt = request.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                query = query.Where(l => l.CreatedAt >= fromDt);
            }

            if (request.To.HasValue)
            {
                var toDt = request.To.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
                query = query.Where(l => l.CreatedAt <= toDt);
            }

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.ToLowerInvariant();
                query = query.Where(l =>
                    l.Email.Contains(search) ||
                    (l.FullName != null && l.FullName.ToLower().Contains(search)) ||
                    (l.CompanyName != null && l.CompanyName.ToLower().Contains(search)));
            }

            // ── Ordering ──────────────────────────────────────────────────────────
            query = request.SortBy?.ToLowerInvariant() switch
            {
                "score" => request.SortDescending
                                        ? query.OrderByDescending(l => l.Score)
                                        : query.OrderBy(l => l.Score),

                "companyname" => request.SortDescending
                                        ? query.OrderByDescending(l => l.CompanyName)
                                        : query.OrderBy(l => l.CompanyName),

                "fullname" => request.SortDescending
                                        ? query.OrderByDescending(l => l.FullName)
                                        : query.OrderBy(l => l.FullName),

                "timetocomplete" => request.SortDescending
                                        ? query.OrderByDescending(l => l.TimeToCompleteSeconds)
                                        : query.OrderBy(l => l.TimeToCompleteSeconds),

                _ => request.SortDescending
                                        ? query.OrderByDescending(l => l.CreatedAt)
                                        : query.OrderBy(l => l.CreatedAt)
            };

            // ── Pagination ────────────────────────────────────────────────────────
            var page     = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);

            var total = await query.CountAsync(ct);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, total);
        }
    }
}
