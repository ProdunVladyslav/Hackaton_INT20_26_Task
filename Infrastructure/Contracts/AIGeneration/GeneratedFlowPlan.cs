using System.Text.Json.Serialization;

namespace Infrastructure.Contracts.AIGeneration
{
    public sealed record GeneratedFlowPlan(
        [property: JsonPropertyName("flow")] GeneratedFlowSpec Flow,
        [property: JsonPropertyName("nodes")] List<GeneratedNodeSpec> Nodes,
        [property: JsonPropertyName("edges")] List<GeneratedEdgeSpec> Edges,
        [property: JsonPropertyName("entryNodeTempId")] string EntryNodeTempId
    );

    public sealed record GeneratedFlowSpec(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("description")] string? Description
    );

    public sealed record GeneratedNodeSpec(
        [property: JsonPropertyName("tempId")] string TempId,
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("positionX")] float PositionX,
        [property: JsonPropertyName("positionY")] float PositionY,

        // Question
        [property: JsonPropertyName("attributeKey")] string? AttributeKey,
        [property: JsonPropertyName("answerType")] string? AnswerType,
        [property: JsonPropertyName("valueKind")] string? ValueKind,
        [property: JsonPropertyName("sliderMin")] decimal? SliderMin,
        [property: JsonPropertyName("sliderMax")] decimal? SliderMax,
        [property: JsonPropertyName("options")] List<GeneratedOptionSpec> Options,

        // Offer
        [property: JsonPropertyName("offer")] GeneratedOfferSpec? Offer,

        // LeadCapture
        [property: JsonPropertyName("isRequired")] bool? IsRequired,
        [property: JsonPropertyName("fields")] List<GeneratedLeadCaptureFieldSpec>? Fields,

        // Redirect
        [property: JsonPropertyName("headline")] string? Headline,
        [property: JsonPropertyName("body")] string? Body,
        [property: JsonPropertyName("disqualificationReason")] string? DisqualificationReason,
        [property: JsonPropertyName("redirectUrl")] string? RedirectUrl,
        [property: JsonPropertyName("autoRedirectAfterSeconds")] int? AutoRedirectAfterSeconds,
        [property: JsonPropertyName("links")] List<GeneratedRedirectLinkSpec>? Links
    );

    public sealed record GeneratedOptionSpec(
        [property: JsonPropertyName("label")] string Label,
        [property: JsonPropertyName("value")] string Value,
        [property: JsonPropertyName("displayOrder")] int DisplayOrder,
        [property: JsonPropertyName("scoreDelta")] int ScoreDelta = 0
    );

    public sealed record GeneratedOfferSpec(
        [property: JsonPropertyName("slug")] string? Slug,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("headline")] string? Headline,
        [property: JsonPropertyName("body")] string? Body,
        [property: JsonPropertyName("imageUrl")] string? ImageUrl,
        [property: JsonPropertyName("calendarUrl")] string? CalendarUrl,
        [property: JsonPropertyName("ctaText")] string? CtaText,
        [property: JsonPropertyName("ctaUrl")] string? CtaUrl,
        [property: JsonPropertyName("tier")] string? Tier,
        [property: JsonPropertyName("calendarProvider")] string? CalendarProvider,
        [property: JsonPropertyName("isPrimary")] bool IsPrimary = true
    );

    public sealed record GeneratedLeadCaptureFieldSpec(
        [property: JsonPropertyName("fieldType")] string FieldType,
        [property: JsonPropertyName("isRequired")] bool IsRequired,
        [property: JsonPropertyName("displayOrder")] int DisplayOrder,
        [property: JsonPropertyName("placeholder")] string? Placeholder
    );

    public sealed record GeneratedRedirectLinkSpec(
        [property: JsonPropertyName("label")] string Label,
        [property: JsonPropertyName("url")] string Url
    );

    public sealed record GeneratedEdgeSpec(
        [property: JsonPropertyName("sourceTempId")] string SourceTempId,
        [property: JsonPropertyName("targetTempId")] string TargetTempId,
        [property: JsonPropertyName("priority")] int Priority,
        [property: JsonPropertyName("conditionsJson")] string? ConditionsJson
    );
}