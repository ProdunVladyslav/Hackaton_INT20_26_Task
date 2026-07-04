using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.LeadChannels.Requests;
using Infrastructure.Contracts.LeadChannels.Responses;

namespace Infrastructure.UseCases.LeadChannels;

public sealed class UpdateLeadChannelUseCase(
    ILeadChannelRepository _leadChannels,
    IFlowRepository _flows,
    IUserProfileRepository _userProfiles,
    IUnitOfWork _uow)
{
    public async Task<FlowResult<LeadChannelResponse>> ExecuteAsync(
        Guid flowId,
        Guid channelId,
        Guid applicationUserId,
        UpdateLeadChannelRequest request,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(
            p => p.ApplicationUserId == applicationUserId, ct);
        if (profile is null)
            return FlowResult<LeadChannelResponse>.NotFound("User profile not found.");

        var flow = await _flows.FirstOrDefaultAsync(
            f => f.Id == flowId && f.OwnerId == profile.Id, ct);
        if (flow is null)
            return FlowResult<LeadChannelResponse>.NotFound("Flow not found.");

        var channel = await _leadChannels.GetByIdAsync(channelId, ct);
        if (channel is null || channel.FlowId != flowId)
            return FlowResult<LeadChannelResponse>.NotFound("Channel not found.");

        try
        {
            if (request.Name is not null)
                channel.Rename(request.Name);

            if (request.IsArchived == true)
                channel.Archive();
            else if (request.IsArchived == false)
                channel.Restore();

            _leadChannels.Update(channel);
            await _uow.SaveChangesAsync(ct);

            var stats = await _leadChannels.GetStatsByFlowAsync(flowId, ct);
            stats.TryGetValue(channel.Id, out var s);

            return FlowResult<LeadChannelResponse>.Ok(CreateLeadChannelUseCase.ToResponse(
                channel,
                sessionCount: s?.SessionCount ?? 0,
                qualifiedCount: s?.QualifiedCount ?? 0,
                disqualifiedCount: s?.DisqualifiedCount ?? 0));
        }
        catch (ArgumentException ex)
        {
            return FlowResult<LeadChannelResponse>.Fail(ex.Message, 400);
        }
    }
}
