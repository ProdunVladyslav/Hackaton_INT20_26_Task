using Application.Repositories.Interfaces;
using Domain.Services;
using Infrastructure.Contracts.Flows.Requests;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Flows;

/// <summary>
/// Use case: update the name and/or description of an existing flow.
/// Partial update — only fields present in the request are applied.
/// </summary>
public sealed class UpdateFlowUseCase(
    IFlowRepository _flows,
    IUserProfileRepository _userProfiles,
    IUnitOfWork _uow,
    IDateTimeProvider _time)
{
    public async Task<FlowResult<FlowSummaryResponse>> ExecuteAsync(
        Guid flowId,
        Guid applicationUserId,
        UpdateFlowRequest request,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(p => p.ApplicationUserId == applicationUserId, ct);
        if (profile == null)
            return FlowResult<FlowSummaryResponse>.NotFound("User profile not found.");

        var flow = await _flows.FirstOrDefaultAsync(f => f.Id == flowId && f.OwnerId == profile.Id, ct);
        if (flow is null)
            return FlowResult<FlowSummaryResponse>.NotFound($"Flow {flowId} not found.");

        try
        {
            if (request.Name is not null)
                flow.SetName(request.Name, _time);

            if (request.Description is not null)
                flow.SetDescription(request.Description, _time);
        }
        catch (ArgumentException ex)
        {
            return FlowResult<FlowSummaryResponse>.Fail(ex.Message, statusCode: 400);
        }

        _flows.Update(flow);
        await _uow.SaveChangesAsync();

        return FlowResult<FlowSummaryResponse>.Ok(FlowMapper.ToSummary(flow));
    }
}