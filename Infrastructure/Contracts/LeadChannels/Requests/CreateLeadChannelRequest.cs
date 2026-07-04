using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Contracts.LeadChannels.Requests
{
    public sealed record CreateLeadChannelRequest(
        [Required] string Name
    );
}
