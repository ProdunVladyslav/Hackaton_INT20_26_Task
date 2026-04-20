using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.Leads.Responses;

namespace Infrastructure.UseCases.Leads;

public sealed class GetLeadUseCase(
    ILeadRepository _leads,
    IUserProfileRepository _userProfiles)
{
    public async Task<FlowResult<LeadResponse>> ExecuteAsync(
        Guid leadId,
        Guid applicationUserId,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(
            p => p.ApplicationUserId == applicationUserId, ct);
        if (profile is null)
            return FlowResult<LeadResponse>.NotFound("User profile not found.");

        var lead = await _leads.GetByIdAsync(leadId, ct);
        if (lead is null || lead.FlowOwnerId != profile.Id)
            return FlowResult<LeadResponse>.NotFound("Lead not found.");

        return FlowResult<LeadResponse>.Ok(ToResponse(lead));
    }

    internal static LeadResponse ToResponse(Domain.Model.User.Lead l) => new(
        Id: l.Id,
        SessionId: l.SessionId,
        FlowId: l.FlowId,
        Email: l.Email,
        FullName: l.FullName,
        Phone: l.Phone,
        CompanyName: l.CompanyName,
        JobTitle: l.JobTitle,
        CompanySize: l.CompanySize,
        Website: l.Website,
        Score: l.Score,
        Tier: l.Tier.ToString(),
        Status: l.Status.ToString(),
        TerminalNodeId: l.TerminalNodeId,
        TerminalNodeType: l.TerminalNodeType.ToString(),
        Notes: l.Notes,
        AssignedToId: l.AssignedToId,
        TimeToCompleteSeconds: l.TimeToCompleteSeconds,
        CreatedAt: l.CreatedAt);
}