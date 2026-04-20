using Application.Repositories.Interfaces;
using Domain.Model.AdminProfile;
using Domain.Model.User;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Flows;

/// <summary>
/// Use case: permanently delete a flow and all its owned data
/// (nodes, edges, options cascade-deleted by DB foreign keys).
/// UserSessions use Restrict delete behavior, so they must be removed explicitly.
/// </summary>
public sealed class DeleteFlowUseCase(
    IFlowRepository _flows,
    IUserSessionRepository _sessions,
    IUserProfileRepository _userProfiles,
    IUnitOfWork _uow)
{
    public async Task<FlowResult<bool>> ExecuteAsync(
        Guid flowId,
        Guid applicationUserId,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(p => p.ApplicationUserId == applicationUserId, ct);
        if (profile == null)
            return FlowResult<bool>.NotFound("User profile not found.");

        var flow = await _flows.FirstOrDefaultAsync(f => f.Id == flowId && f.OwnerId == profile.Id, ct);

        if (flow is null)
            return FlowResult<bool>.NotFound($"Flow {flowId} not found.");

        // Remove UserSessions first (FK is Restrict, not Cascade).
        // UserAnswer and SessionOffer cascade from UserSession.
        var sessions = await _sessions.FindAsync(s => s.FlowId == flowId, ct);
        _sessions.RemoveRange(sessions);

        _flows.Remove(flow);
        await _uow.SaveChangesAsync();

        return FlowResult<bool>.Ok(true);
    }
}
