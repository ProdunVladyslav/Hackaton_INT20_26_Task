namespace Infrastructure.Contracts.LeadChannels.Requests
{
    public sealed record UpdateLeadChannelRequest(
        string? Name = null,
        bool? IsArchived = null
    );
}
