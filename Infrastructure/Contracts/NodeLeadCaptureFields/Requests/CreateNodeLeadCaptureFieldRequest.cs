using Domain.Model.Survey;

namespace Infrastructure.Contracts.NodeLeadCaptureFields.Requests
{
    public sealed record CreateNodeLeadCaptureFieldRequest(
        string FieldType,
        bool IsRequired,
        int DisplayOrder,
        string Placeholder = "");
}
