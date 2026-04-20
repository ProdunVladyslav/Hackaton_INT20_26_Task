using Domain.Model.Survey;

namespace Infrastructure.Contracts.NodeLeadCaptureFields.Responses
{
    public sealed record NodeLeadCaptureFieldResponse(
        Guid Id,
        Guid NodeLeadCaptureId,
        LeadCaptureFieldType FieldType,
        string AttributeKey,
        bool IsRequired,
        int DisplayOrder,
        string Placeholder);
}
