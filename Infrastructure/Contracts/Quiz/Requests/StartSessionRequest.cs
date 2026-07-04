using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Contracts.Quiz.Requests;

public sealed record StartSessionRequest(
    [Required] Guid FlowId,
    string? UtmSource = null,
    string? UtmCampaign = null,
    Guid? LeadChannelId = null);
