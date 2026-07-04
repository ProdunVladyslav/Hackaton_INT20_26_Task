using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.LeadChannels.Responses;

namespace Infrastructure.UseCases.LeadChannels;

/// <summary>
/// Public, unauthenticated — resolves a short-link code clicked by a
/// respondent to the flow (and channel) it should attribute to.
/// </summary>
public sealed class ResolveLeadChannelUseCase(
    ILeadChannelRepository _leadChannels,
    IFlowRepository _flows)
{
    public async Task<FlowResult<LeadChannelResolveResponse>> ExecuteAsync(
        string shortCode,
        CancellationToken ct = default)
    {
        var channel = await _leadChannels.GetByShortCodeAsync(shortCode, ct);
        if (channel is null || channel.IsArchived)
            return FlowResult<LeadChannelResolveResponse>.NotFound("Link not found.");

        var flow = await _flows.GetByIdAsync(channel.FlowId, ct);
        if (flow is null || !flow.IsPublished)
            return FlowResult<LeadChannelResolveResponse>.NotFound("Link not found.");

        return FlowResult<LeadChannelResolveResponse>.Ok(
            new LeadChannelResolveResponse(flow.Id, channel.Id));
    }
}
