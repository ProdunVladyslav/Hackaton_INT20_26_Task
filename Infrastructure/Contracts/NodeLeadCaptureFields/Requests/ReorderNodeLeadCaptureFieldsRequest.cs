using Domain.Model.Survey;

namespace Infrastructure.Contracts.NodeLeadCaptureFields.Requests
{
    public sealed record ReorderNodeLeadCaptureFieldsRequest(
    List<ReorderNodeLeadCaptureFieldItem> Items);

    public sealed record ReorderNodeLeadCaptureFieldItem(
        string FieldType,
        int DisplayOrder);
}
