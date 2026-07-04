using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Contracts.Leads.Responses
{
    public sealed record PagedLeadResponse(
        List<LeadResponse> Items,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages
    );


    public sealed record LeadResponse(
        Guid Id,
        Guid SessionId,
        Guid FlowId,
        string Email,
        string? FullName,
        string? Phone,
        string? CompanyName,
        string? JobTitle,
        string? CompanySize,
        string? Website,
        int Score,
        string Tier,
        string Status,
        Guid TerminalNodeId,
        string TerminalNodeType,
        string? Notes,
        Guid? AssignedToId,
        int TimeToCompleteSeconds,
        DateTime CreatedAt,
        Guid? LeadChannelId
    );
}
