using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Flows.Requests;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Flows;

/// <summary>
/// Use case: create a new, unpublished flow with the given name and description.
/// The flow starts with no nodes, no edges, and no entry node.
/// </summary>
public sealed class CreateFlowUseCase(
    IFlowRepository _flows,
    IUserProfileRepository _userProfiles,
    IUnitOfWork _uow)
{
    public async Task<FlowResult<FlowSummaryResponse>> ExecuteAsync(
        CreateFlowRequest request,
        Guid applicationUserId,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(p => p.ApplicationUserId == applicationUserId, ct);
        if (profile == null)
            return FlowResult<FlowSummaryResponse>.NotFound("User profile not found.");

        // Guard: name uniqueness is not required by spec but we validate it's not blank
        // (the DTO attribute already covers this, but we enforce in the domain too).
        var flow = Flow.Create(request.Name, request.Description ?? string.Empty, profile.Id);

        await _flows.AddAsync(flow, ct);
        await _uow.SaveChangesAsync();

        return FlowResult<FlowSummaryResponse>.Ok(FlowMapper.ToSummary(flow));
    }
}
