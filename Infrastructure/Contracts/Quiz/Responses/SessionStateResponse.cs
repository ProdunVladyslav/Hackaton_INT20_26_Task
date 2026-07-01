namespace Infrastructure.Contracts.Quiz.Responses;

public sealed record SessionStateResponse(
    Guid SessionId,
    Guid FlowId,
    string Status,
    DateTime StartedAt,
    DateTime? CompletedAt,
    CurrentNodeResponse? CurrentNode
);

public sealed record CurrentNodeResponse(
    Guid Id,
    string Type,
    string? AttributeKey,
    string? AnswerType,
    string? ValueKind,
    decimal? SliderMin,
    decimal? SliderMax,
    string Title,
    string? Description,
    string? MediaUrl,
    List<QuizOptionResponse> Options,
    List<QuizOfferResponse> Offers,
    QuizLeadCaptureResponse? LeadCapture,  // populated when Type == LeadCapture
    QuizRedirectResponse? Redirect         // populated when Type == Redirect
);
public sealed record QuizOptionResponse(
    Guid Id,
    string Label,
    string Value,
    int DisplayOrder,
    string? MediaUrl,
    int ScoreDelta
);

public sealed record QuizOfferResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Headline,
    string? Body,
    string? ImageUrl,
    string? CalendarUrl,
    string? CalendarProvider,
    string? CtaText,
    string? CtaUrl,
    bool IsPrimary,
    string Tier
);

public sealed record QuizLeadCaptureResponse(
    bool IsRequired,
    IReadOnlyList<QuizLeadCaptureFieldResponse> Fields
);

public sealed record QuizLeadCaptureFieldResponse(
    string FieldType,
    string AttributeKey,
    bool IsRequired,
    int DisplayOrder,
    string? Placeholder
);

public sealed record QuizRedirectResponse(
    string? RedirectUrl,
    int? AutoRedirectAfterSeconds,
    string? DisqualificationReason,
    IReadOnlyList<QuizRedirectLinkResponse> Links
);

public sealed record QuizRedirectLinkResponse(
    string Label,
    string Url,
    int DisplayOrder
);