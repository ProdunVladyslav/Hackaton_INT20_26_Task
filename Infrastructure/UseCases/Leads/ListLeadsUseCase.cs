using Application.Contracts.Leads;
using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.Leads.Responses;

namespace Infrastructure.UseCases.Leads;

public sealed class ListLeadsUseCase(
    ILeadRepository _leads,
    IFlowRepository _flows,
    IUserProfileRepository _userProfiles)
{
    public async Task<FlowResult<PagedLeadResponse>> ExecuteAsync(
        Guid flowId,
        Guid applicationUserId,
        ListLeadsRequest request,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(
            p => p.ApplicationUserId == applicationUserId, ct);
        if (profile is null)
            return FlowResult<PagedLeadResponse>.NotFound("User profile not found.");

        var flow = await _flows.FirstOrDefaultAsync(
            f => f.Id == flowId && f.OwnerId == profile.Id, ct);
        if (flow is null)
            return FlowResult<PagedLeadResponse>.NotFound("Flow not found.");

        var page     = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var (items, total) = await _leads.GetByFlowFilteredAsync(flowId, request, ct);

        return FlowResult<PagedLeadResponse>.Ok(new PagedLeadResponse(
            Items:      items.Select(GetLeadUseCase.ToResponse).ToList(),
            TotalCount: total,
            Page:       page,
            PageSize:   pageSize,
            TotalPages: (int)Math.Ceiling(total / (double)pageSize)));
    }
}