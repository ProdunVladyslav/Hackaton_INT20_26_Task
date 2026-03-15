using Infrastructure.Contracts.Offers.Responses;

namespace Infrastructure.Contracts.Nodes.Responses;

public sealed record NodeResponse(
    Guid Id,
    Guid FlowId,
    string Type,
    string? AttributeKey,
    string Title,
    string? Description,
    string? MediaUrl,
    float PositionX,
    float PositionY,
    DateTime CreatedAt,
    string? AnswerType,
    decimal? SliderMin,
    decimal? SliderMax,
    /// <summary>Populated only when an inline offer was created alongside this node.</summary>
    OfferResponse? LinkedOffer = null
);
