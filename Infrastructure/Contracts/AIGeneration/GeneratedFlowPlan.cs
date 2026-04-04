using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
        [property: JsonPropertyName("attributeKey")] string? AttributeKey,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("positionX")] float PositionX,
        [property: JsonPropertyName("positionY")] float PositionY,
        [property: JsonPropertyName("answerType")] string? AnswerType,
        [property: JsonPropertyName("valueKind")] string? ValueKind,
        [property: JsonPropertyName("sliderMin")] decimal? SliderMin,
        [property: JsonPropertyName("sliderMax")] decimal? SliderMax,
        [property: JsonPropertyName("options")] List<GeneratedOptionSpec> Options,
        [property: JsonPropertyName("offer")] GeneratedOfferSpec? Offer
    );

    public sealed record GeneratedOptionSpec(
        [property: JsonPropertyName("label")] string Label,
        [property: JsonPropertyName("value")] string Value,
        [property: JsonPropertyName("displayOrder")] int DisplayOrder
    );

    public sealed record GeneratedOfferSpec(
        [property: JsonPropertyName("slug")] string? Slug,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("duration")] string? Duration,
        [property: JsonPropertyName("digitalContent")] string? DigitalContent,
        [property: JsonPropertyName("physicalWellnessKitName")] string? PhysicalWellnessKitName,
        [property: JsonPropertyName("physicalWellnessKitItems")] string? PhysicalWellnessKitItems,
        [property: JsonPropertyName("price")] decimal? Price,
        [property: JsonPropertyName("imageUrl")] string? ImageUrl,
        [property: JsonPropertyName("ctaText")] string? CtaText,
        [property: JsonPropertyName("ctaUrl")] string? CtaUrl,
        [property: JsonPropertyName("isPrimary")] bool IsPrimary
    );

    public sealed record GeneratedEdgeSpec(
        [property: JsonPropertyName("sourceTempId")] string SourceTempId,
        [property: JsonPropertyName("targetTempId")] string TargetTempId,
        [property: JsonPropertyName("priority")] int Priority,
        [property: JsonPropertyName("conditionsJson")] string? ConditionsJson
    );
}
