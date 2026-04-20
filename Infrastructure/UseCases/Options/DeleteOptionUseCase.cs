using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Options;

/// <summary>
/// Use case: delete an option from a node.
/// </summary>
public sealed class DeleteOptionUseCase(
    IOptionRepository _options,
    INodeRepository _nodes,
    IFlowRepository _flows,
    IUserProfileRepository _userProfiles,
    IUnitOfWork _uow)
{
    public async Task<FlowResult<bool>> ExecuteAsync(
        Guid nodeId,
        Guid optionId,
        Guid applicationUserId,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(p => p.ApplicationUserId == applicationUserId, ct);
        if (profile == null)
            return FlowResult<bool>.NotFound("User profile not found.");

        var node = await _nodes.GetByIdAsync(nodeId, ct);
        if (node == null)
            return FlowResult<bool>.NotFound("Node not found.");

        var flow = await _flows.FirstOrDefaultAsync(f => f.Id == node.FlowId && f.OwnerId == profile.Id, ct);
        if (flow == null)
            return FlowResult<bool>.NotFound("Node not found.");

        var option = await _options.GetByIdAsync(optionId, ct);
        if (option == null)
            return FlowResult<bool>.NotFound("Option not found.");

        if (option.NodeId != nodeId)
            return FlowResult<bool>.NotFound("Option not found in this node.");

        _options.Remove(option);
        await _uow.SaveChangesAsync(ct);

        return FlowResult<bool>.Ok(true);
    }
}
