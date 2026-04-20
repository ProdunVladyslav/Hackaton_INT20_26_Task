namespace Infrastructure.Contracts.NodeLeadCaptureFields.Requests
{
    public sealed record UpdateNodeLeadCaptureFieldRequest(
        bool? IsRequired,
        int? DisplayOrder,
        string? Placeholder);
}
