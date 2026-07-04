using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Domain.Services;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.LeadChannels.Requests;
using Infrastructure.Contracts.LeadChannels.Responses;
using Infrastructure.Services.Interfaces;

namespace Infrastructure.UseCases.LeadChannels;

public sealed class CreateLeadChannelUseCase(
    ILeadChannelRepository _leadChannels,
    IFlowRepository _flows,
    IUserProfileRepository _userProfiles,
    IShortCodeGenerator _shortCodes,
    IUnitOfWork _uow,
    IDateTimeProvider _time)
{
    private const int MaxShortCodeAttempts = 5;

    public async Task<FlowResult<LeadChannelResponse>> ExecuteAsync(
        Guid flowId,
        Guid applicationUserId,
        CreateLeadChannelRequest request,
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

        if (string.IsNullOrWhiteSpace(request.Name))
            return FlowResult<LeadChannelResponse>.Fail("Channel name is required.", 400);

        string? shortCode = null;
        for (var attempt = 0; attempt < MaxShortCodeAttempts; attempt++)
        {
            var candidate = _shortCodes.Generate();
            if (!await _leadChannels.ExistsByShortCodeAsync(candidate, ct))
            {
                shortCode = candidate;
                break;
            }
        }

        if (shortCode is null)
            return FlowResult<LeadChannelResponse>.Fail(
                "Could not generate a unique short code. Try again.", 500);

        var channel = LeadChannel.Create(flowId, request.Name, shortCode, _time);
        await _leadChannels.AddAsync(channel, ct);
        await _uow.SaveChangesAsync(ct);

        return FlowResult<LeadChannelResponse>.Ok(ToResponse(channel, sessionCount: 0, qualifiedCount: 0, disqualifiedCount: 0));
    }

    internal static LeadChannelResponse ToResponse(
        LeadChannel c, int sessionCount, int qualifiedCount, int disqualifiedCount) => new(
        Id: c.Id,
        FlowId: c.FlowId,
        Name: c.Name,
        ShortCode: c.ShortCode,
        IsArchived: c.IsArchived,
        CreatedAt: c.CreatedAt,
        SessionCount: sessionCount,
        QualifiedCount: qualifiedCount,
        DisqualifiedCount: disqualifiedCount,
        QualificationRate: sessionCount > 0 ? Math.Round((double)qualifiedCount / sessionCount, 4) : 0d);
}
