using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.NodeLeadCaptureFields;

/// <summary>
/// Removes a field identified by FieldType from the LeadCapture node.
/// </summary>
public sealed class DeleteNodeLeadCaptureFieldUseCase(
    INodeRepository _nodes,
    INodeLeadCaptureFieldRepository _fields,
    IFlowRepository _flows,
    IUserProfileRepository _userProfiles,
    IUnitOfWork _uow)
{
    public async Task<FlowResult<bool>> ExecuteAsync(
        Guid nodeId,
        LeadCaptureFieldType fieldType,
        Guid applicationUserId,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(
            p => p.ApplicationUserId == applicationUserId, ct);
        if (profile == null)
            return FlowResult<bool>.NotFound("User profile not found.");

        var node = await _nodes.GetByIdAsync(nodeId, ct);
        if (node == null)
            return FlowResult<bool>.NotFound("Node not found.");

        var flow = await _flows.FirstOrDefaultAsync(
            f => f.Id == node.FlowId && f.OwnerId == profile.Id, ct);
        if (flow == null)
            return FlowResult<bool>.NotFound("Node not found.");

        if (node.Type != NodeType.LeadCapture)
            return FlowResult<bool>.Fail(
                "Only LeadCapture nodes can have lead capture fields.", statusCode: 422);

        if (node.LeadCapture == null)
            return FlowResult<bool>.NotFound("LeadCapture configuration not found on this node.");

        var field = node.LeadCapture.Fields.FirstOrDefault(f => f.FieldType == fieldType);
        if (field == null)
            return FlowResult<bool>.NotFound($"Field '{fieldType}' not found on this node.");

        _fields.Remove(field);
        await _uow.SaveChangesAsync(ct);

        return FlowResult<bool>.Ok(true);
    }
}