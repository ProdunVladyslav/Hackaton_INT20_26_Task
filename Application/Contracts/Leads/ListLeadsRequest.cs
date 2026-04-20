using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts.Leads
{
    public sealed record ListLeadsRequest(
        string? Tier = null,
        string? Status = null,
        string? Search = null,
        DateOnly? From = null,
        DateOnly? To = null,
        string? SortBy = null,       // CreatedAt | Score | CompanyName | FullName | TimeToComplete
        bool SortDescending = true,  // default: newest first
        int Page = 1,
        int PageSize = 20
    );
}
