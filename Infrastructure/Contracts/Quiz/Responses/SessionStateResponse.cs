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
    List<QuizOfferResponse> Offers
);

public sealed record QuizOptionResponse(Guid Id, string Label, string Value, int DisplayOrder, string? MediaUrl);

public sealed record QuizOfferResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? Duration,
    string? DigitalContent,
    string? PhysicalWellnessKitName,
    string? PhysicalWellnessKitItems,
    decimal? Price,
    string? ImageUrl,
    string? CtaText,
    string? CtaUrl,
    bool IsPrimary
);
