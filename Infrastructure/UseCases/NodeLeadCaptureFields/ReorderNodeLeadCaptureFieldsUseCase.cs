using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.NodeLeadCaptureFields.Requests;
using Infrastructure.Contracts.NodeLeadCaptureFields.Responses;

namespace Infrastructure.UseCases.NodeLeadCaptureFields;

/// <summary>
/// Bulk-updates DisplayOrder for fields on a LeadCapture node.
/// Fields are identified by FieldType. Unknown types are silently skipped.
/// </summary>
public sealed class ReorderNodeLeadCaptureFieldsUseCase(
    INodeRepository _nodes,
    INodeLeadCaptureFieldRepository _fields,
    IFlowRepository _flows,
    IUserProfileRepository _userProfiles,
    IUnitOfWork _uow)
{
    public async Task<FlowResult<List<NodeLeadCaptureFieldResponse>>> ExecuteAsync(
        Guid nodeId,
        Guid applicationUserId,
        ReorderNodeLeadCaptureFieldsRequest request,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(
            p => p.ApplicationUserId == applicationUserId, ct);
        if (profile == null)
            return FlowResult<List<NodeLeadCaptureFieldResponse>>.NotFound("User profile not found.");

        var node = await _nodes.GetByIdAsync(nodeId, ct);
        if (node == null)
            return FlowResult<List<NodeLeadCaptureFieldResponse>>.NotFound("Node not found.");

        var flow = await _flows.FirstOrDefaultAsync(
            f => f.Id == node.FlowId && f.OwnerId == profile.Id, ct);
        if (flow == null)
            return FlowResult<List<NodeLeadCaptureFieldResponse>>.NotFound("Node not found.");

        if (node.Type != NodeType.LeadCapture)
            return FlowResult<List<NodeLeadCaptureFieldResponse>>.Fail(
                "Only LeadCapture nodes can have lead capture fields.", statusCode: 422);

        if (node.LeadCapture == null)
            return FlowResult<List<NodeLeadCaptureFieldResponse>>.NotFound(
                "LeadCapture configuration not found on this node.");

        try
        {
            foreach (var item in request.Items)
            {
                var field = node.LeadCapture.Fields.FirstOrDefault(f => f.FieldType == Enum.Parse<LeadCaptureFieldType>(item.FieldType));
                if (field == null) continue;

                field.SetOrder(item.DisplayOrder);
                _fields.Update(field);
            }

            await _uow.SaveChangesAsync(ct);

            var reordered = node.LeadCapture.Fields
                .OrderBy(f => f.DisplayOrder)
                .ToList();

            return FlowResult<List<NodeLeadCaptureFieldResponse>>.Ok(
                reordered.Select(ToResponse).ToList());
        }
        catch (ArgumentException ex)
        {
            return FlowResult<List<NodeLeadCaptureFieldResponse>>.Fail(ex.Message, statusCode: 400);
        }
    }

    private static NodeLeadCaptureFieldResponse ToResponse(NodeLeadCaptureField f) =>
        new(f.Id, f.NodeLeadCaptureId, f.FieldType, f.AttributeKey,
            f.IsRequired, f.DisplayOrder, f.Placeholder);
}