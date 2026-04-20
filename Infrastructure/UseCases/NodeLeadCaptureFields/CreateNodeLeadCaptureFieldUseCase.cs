using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.NodeLeadCaptureFields.Requests;
using Infrastructure.Contracts.NodeLeadCaptureFields.Responses;

namespace Infrastructure.UseCases.NodeLeadCaptureFields;

/// <summary>
/// Adds a new field to the LeadCapture node's NodeLeadCapture aggregate.
/// Each FieldType may appear at most once; max 7 fields total.
/// Email is always required (domain enforced).
/// </summary>
public sealed class CreateNodeLeadCaptureFieldUseCase(
    INodeRepository _nodes,
    INodeLeadCaptureFieldRepository _fields,
    IFlowRepository _flows,
    IUserProfileRepository _userProfiles,
    IUnitOfWork _uow)
{
    public async Task<FlowResult<NodeLeadCaptureFieldResponse>> ExecuteAsync(
        Guid nodeId,
        Guid applicationUserId,
        CreateNodeLeadCaptureFieldRequest request,
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

        try
        {
            var field = NodeLeadCaptureField.Create(
                node.LeadCapture.Id,
                Enum.Parse<LeadCaptureFieldType>(request.FieldType),
                request.IsRequired,
                request.DisplayOrder,
                request.Placeholder);

            // Domain enforces uniqueness per FieldType and max-7 rule
            node.LeadCapture.AddField(field);

            await _fields.AddAsync(field, ct);
            await _uow.SaveChangesAsync(ct);

            return FlowResult<NodeLeadCaptureFieldResponse>.Ok(ToResponse(field));
        }
        catch (ArgumentException ex)
        {
            return FlowResult<NodeLeadCaptureFieldResponse>.Fail(ex.Message, statusCode: 400);
        }
        catch (InvalidOperationException ex)
        {
            return FlowResult<NodeLeadCaptureFieldResponse>.Fail(ex.Message, statusCode: 422);
        }
    }

    private static NodeLeadCaptureFieldResponse ToResponse(NodeLeadCaptureField f) =>
        new(f.Id, f.NodeLeadCaptureId, f.FieldType, f.AttributeKey,
            f.IsRequired, f.DisplayOrder, f.Placeholder);
}