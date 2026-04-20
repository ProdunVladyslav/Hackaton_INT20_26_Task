using Application.Repositories.Interfaces;
using Domain.Model.Auth;
using Infrastructure.Contracts.Edges.Responses;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Edges;

/// <summary>
/// Use case: delete an edge from a flow.
/// </summary>
public sealed class DeleteEdgeUseCase(
    IEdgeRepository _edges,
    IUserProfileRepository _userProfiles,
    IUnitOfWork _uow)
{
    public async Task<FlowResult<bool>> ExecuteAsync(
        Guid flowId,
        Guid applicationUserId,
        Guid edgeId,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(p => p.ApplicationUserId == applicationUserId, ct);
        if (profile == null)
            return FlowResult<bool>.NotFound("User profile not found.");

        // Load edge
        var edge = await _edges.GetByIdWithOwnerCheckAsync(edgeId, profile.Id, ct);
        if (edge == null)
            return FlowResult<bool>.Ok(true);

        // Verify edge belongs to the flow
        if (edge.FlowId != flowId)
            return FlowResult<bool>.NotFound("Edge not found in this flow.");

        // Delete edge
        _edges.Remove(edge);
        await _uow.SaveChangesAsync();

        return FlowResult<bool>.Ok(true);
    }
}
