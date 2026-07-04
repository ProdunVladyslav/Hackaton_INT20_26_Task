using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Flows.Responses;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.UseCases.LeadChannels;

public sealed class DeleteLeadChannelUseCase(
    ILeadChannelRepository _leadChannels,
    IFlowRepository _flows,
    IUserProfileRepository _userProfiles,
    IUnitOfWork _uow)
{
    public async Task<FlowResult<bool>> ExecuteAsync(
        Guid flowId,
        Guid channelId,
        Guid applicationUserId,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(
            p => p.ApplicationUserId == applicationUserId, ct);
        if (profile is null)
            return FlowResult<bool>.NotFound("User profile not found.");

        var flow = await _flows.FirstOrDefaultAsync(
            f => f.Id == flowId && f.OwnerId == profile.Id, ct);
        if (flow is null)
            return FlowResult<bool>.NotFound("Flow not found.");

        var channel = await _leadChannels.GetByIdAsync(channelId, ct);
        if (channel is null || channel.FlowId != flowId)
            return FlowResult<bool>.NotFound("Channel not found.");

        try
        {
            _leadChannels.Remove(channel);
            await _uow.SaveChangesAsync(ct);

            return FlowResult<bool>.Ok(true);
        }
        catch (DbUpdateException)
        {
            return FlowResult<bool>.Fail(
                "This channel already has attributed sessions. Archive it instead of deleting.", 409);
        }
    }
}
