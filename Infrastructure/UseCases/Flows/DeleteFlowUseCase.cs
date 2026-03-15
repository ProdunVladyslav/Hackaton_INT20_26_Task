using Application.Repositories.Interfaces;
using Domain.Model.User;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Flows;

/// <summary>
/// Use case: permanently delete a flow and all its owned data
/// (nodes, edges, options cascade-deleted by DB foreign keys).
/// UserSessions use Restrict delete behavior, so they must be removed explicitly.
/// </summary>
public sealed class DeleteFlowUseCase
{
    private readonly IFlowRepository        _flows;
    private readonly IUserSessionRepository _sessions;
    private readonly IUnitOfWork            _uow;

    public DeleteFlowUseCase(
        IFlowRepository flows,
        IUserSessionRepository sessions,
        IUnitOfWork uow)
    {
        _flows    = flows;
        _sessions = sessions;
        _uow      = uow;
    }

    public async Task<FlowResult<bool>> ExecuteAsync(
        Guid flowId,
        CancellationToken ct = default)
    {
        var flow = await _flows.GetByIdAsync(flowId, ct);

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
