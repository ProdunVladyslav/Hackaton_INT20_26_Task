using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.NodeLeadCaptureFields.Requests;
using Infrastructure.Contracts.NodeLeadCaptureFields.Responses;

namespace Infrastructure.UseCases.NodeLeadCaptureFields;

/// <summary>
/// Patch-style update for a single lead capture field identified by FieldType.
/// Only provided fields are applied.
/// Email's IsRequired cannot be set to false (domain enforced).
/// </summary>
public sealed class UpdateNodeLeadCaptureFieldUseCase(
    INodeRepository _nodes,
    INodeLeadCaptureFieldRepository _fields,
    IFlowRepository _flows,
    IUserProfileRepository _userProfiles,
    IUnitOfWork _uow)
{
    public async Task<FlowResult<NodeLeadCaptureFieldResponse>> ExecuteAsync(
        Guid nodeId,
        LeadCaptureFieldType fieldType,
        Guid applicationUserId,
        UpdateNodeLeadCaptureFieldRequest request,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(
            p => p.ApplicationUserId == applicationUserId, ct);
        if (profile == null)
            return FlowResult<NodeLeadCaptureFieldResponse>.NotFound("User profile not found.");

        var node = await _nodes.GetByIdAsync(nodeId, ct);
        if (node == null)
            return FlowResult<NodeLeadCaptureFieldResponse>.NotFound("Node not found.");

        var flow = await _flows.FirstOrDefaultAsync(
            f => f.Id == node.FlowId && f.OwnerId == profile.Id, ct);
        if (flow == null)
            return FlowResult<NodeLeadCaptureFieldResponse>.NotFound("Node not found.");

        if (node.Type != NodeType.LeadCapture)
            return FlowResult<NodeLeadCaptureFieldResponse>.Fail(
                "Only LeadCapture nodes can have lead capture fields.", statusCode: 422);

        if (node.LeadCapture == null)
            return FlowResult<NodeLeadCaptureFieldResponse>.NotFound(
                "LeadCapture configuration not found on this node.");

        var field = node.LeadCapture.Fields.FirstOrDefault(f => f.FieldType == fieldType);
        if (field == null)
            return FlowResult<NodeLeadCaptureFieldResponse>.NotFound(
                $"Field '{fieldType}' not found on this node.");

        try
        {
            if (request.Placeholder is not null)
                field.SetPlaceholder(request.Placeholder);

            if (request.DisplayOrder.HasValue)
                field.SetOrder(request.DisplayOrder.Value);

            if (request.IsRequired.HasValue)
            {
                if (field.FieldType == LeadCaptureFieldType.Email && !request.IsRequired.Value)
                    return FlowResult<NodeLeadCaptureFieldResponse>.Fail(
                        "Email field must always be required.", statusCode: 400);

                field.SetIsRequired(request.IsRequired.Value);
            }

            _fields.Update(field);
            await _uow.SaveChangesAsync(ct);

            return FlowResult<NodeLeadCaptureFieldResponse>.Ok(ToResponse(field));
        }
        catch (ArgumentException ex)
        {
            return FlowResult<NodeLeadCaptureFieldResponse>.Fail(ex.Message, statusCode: 400);
        }
    }

    private static NodeLeadCaptureFieldResponse ToResponse(NodeLeadCaptureField f) =>
        new(f.Id, f.NodeLeadCaptureId, f.FieldType, f.AttributeKey,
            f.IsRequired, f.DisplayOrder, f.Placeholder);
}