using Domain.Model.Survey;

namespace Infrastructure.Contracts.Nodes.Requests;

public sealed record UpdateNodeRequest(
    string? Title = null,
    string? Description = null,
    string? MediaUrl = null,
    // Question
    string? AnswerType = null,
    decimal? SliderMin = null,
    decimal? SliderMax = null,
    bool ClearAnswerType = false,
    // Offer
    UpdateInlineOfferRequest? Offer = null,
    // Redirect
    string? DisqualificationReason = null,
    string? RedirectUrl = null,
    int? AutoRedirectAfterSeconds = null,
    // LeadCapture
    bool? IsRequired = null
);

public sealed record UpdateInlineOfferRequest(
    string? Headline = null,
    string? Body = null,
    string? ImageUrl = null,
    string? CalendarUrl = null,
    string? Tier = null,
    string? CalendarProvider = null,
    string? CtaText = null,
    string? CtaUrl = null
);