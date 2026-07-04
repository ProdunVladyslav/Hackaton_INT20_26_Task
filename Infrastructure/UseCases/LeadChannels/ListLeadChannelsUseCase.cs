using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.LeadChannels.Responses;

namespace Infrastructure.UseCases.LeadChannels;

public sealed class ListLeadChannelsUseCase(
    ILeadChannelRepository _leadChannels,
    IFlowRepository _flows,
    IUserProfileRepository _userProfiles)
{
    public async Task<FlowResult<List<LeadChannelResponse>>> ExecuteAsync(
        Guid flowId,
        Guid applicationUserId,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(
            p => p.ApplicationUserId == applicationUserId, ct);
        if (profile is null)
            return FlowResult<List<LeadChannelResponse>>.NotFound("User profile not found.");

        var flow = await _flows.FirstOrDefaultAsync(
            f => f.Id == flowId && f.OwnerId == profile.Id, ct);
        if (flow is null)
            return FlowResult<List<LeadChannelResponse>>.NotFound("Flow not found.");

        var channels = await _leadChannels.GetByFlowIdAsync(flowId, ct);
        var stats = await _leadChannels.GetStatsByFlowAsync(flowId, ct);

        var items = channels.Select(c =>
        {
            stats.TryGetValue(c.Id, out var s);
            return CreateLeadChannelUseCase.ToResponse(
                c,
                sessionCount: s?.SessionCount ?? 0,
                qualifiedCount: s?.QualifiedCount ?? 0,
                disqualifiedCount: s?.DisqualifiedCount ?? 0);
        }).ToList();

        return FlowResult<List<LeadChannelResponse>>.Ok(items);
    }
}
