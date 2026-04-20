using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Domain.Model.User;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.Leads.Requests;
using Infrastructure.Contracts.Leads.Responses;

namespace Infrastructure.UseCases.Leads;

public sealed class UpdateLeadUseCase(
    ILeadRepository _leads,
    IUserProfileRepository _userProfiles,
    IUnitOfWork _uow)
{
    public async Task<FlowResult<LeadResponse>> ExecuteAsync(
        Guid leadId,
        Guid applicationUserId,
        UpdateLeadRequest request,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(
            p => p.ApplicationUserId == applicationUserId, ct);
        if (profile is null)
            return FlowResult<LeadResponse>.NotFound("User profile not found.");

        var lead = await _leads.GetByIdAsync(leadId, ct);
        if (lead is null || lead.FlowOwnerId != profile.Id)
            return FlowResult<LeadResponse>.NotFound("Lead not found.");

        try
        {
            if (request.Status is not null
                && Enum.TryParse<LeadStatus>(request.Status, ignoreCase: true, out var status))
                lead.SetStatus(status);

            if (request.Tier is not null
                && Enum.TryParse<QualificationTier>(request.Tier, ignoreCase: true, out var tier))
                lead.UpdateTier(tier);

            if (request.Notes is not null)
                lead.SetNotes(request.Notes);

            // AssignedToId: explicit null in request = clear assignment
            if (request.UpdateAssignment)
                lead.AssignTo(request.AssignedToId);

            _leads.Update(lead);
            await _uow.SaveChangesAsync(ct);

            return FlowResult<LeadResponse>.Ok(GetLeadUseCase.ToResponse(lead));
        }
        catch (ArgumentException ex)
        {
            return FlowResult<LeadResponse>.Fail(ex.Message, 400);
        }
    }
}