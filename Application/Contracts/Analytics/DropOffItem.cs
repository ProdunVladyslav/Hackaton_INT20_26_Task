namespace Application.Contracts.Analytics
{
    public sealed record DropOffItem(
        Guid NodeId,
        string NodeTitle,
        Guid FlowId,
        string FlowTitle,
        int SessionCount,
        double DropOffRate);
}
