using Application.Contracts.Analytics;
using Domain.Model.Survey;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Flows;

/// <summary>
/// Stateless projection helpers: Domain → DTO.
/// Keeping mapping logic here avoids duplicating it across every use case.
/// </summary>
public static class FlowMapper
{
    /// <summary>Maps a Flow to its lightweight summary representation (no nodes/edges).</summary>
    public static FlowSummaryResponse ToSummary(Flow flow, FlowAdminStats? stats = null) =>
        new(
            Id: flow.Id,
            Name: flow.Name,
            Description: flow.Description,
            IsPublished: flow.IsPublished,
            EntryNodeId: flow.EntryNodeId,
            CreatedAt: flow.CreatedAt,
            UpdatedAt: flow.UpdatedAt,
            Stats: stats
        );

    /// <summary>Maps a Flow (with fully loaded DAG) to the detailed response.</summary>
    public static FlowDetailResponse ToDetail(Flow flow) =>
        new(
            Id: flow.Id,
            Name: flow.Name,
            Description: flow.Description,
            IsPublished: flow.IsPublished,
            EntryNodeId: flow.EntryNodeId,
            CreatedAt: flow.CreatedAt,
            UpdatedAt: flow.UpdatedAt,
            Nodes: flow.Nodes.Select(ToNodeDto).ToList(),
            Edges: flow.Edges.Select(ToEdgeDto).ToList(),
            AttributeKeys: []
        );

    private static NodeDto ToNodeDto(Node node) =>
        new(
            Id: node.Id,
            Type: node.Type.ToString(),
            AttributeKey: node.AttributeKey,
            Title: node.Title,
            Description: node.Description,
            MediaUrl: node.MediaUrl,
            PositionX: node.PositionX,
            PositionY: node.PositionY,
            CreatedAt: node.CreatedAt,
            AnswerType: node.AnswerType?.ToString(),
            ValueKind: node.ValueKind?.ToString(),
            SliderMin: node.SliderMin,
            SliderMax: node.SliderMax,
            Options: node.Options.Select(ToOptionDto).ToList(),
            NodeOffers: [],  // enriched separately in use case
            Redirect: null, // enriched separately in use case
            LeadCapture: null  // enriched separately in use case
        );

    private static OptionDto ToOptionDto(Option opt) =>
        new(
            Id: opt.Id,
            Label: opt.Label,
            Value: opt.Value,
            ScoreDelta: opt.ScoreDelta,
            DisplayOrder: opt.DisplayOrder,
            MediaUrl: opt.MediaUrl
        );

    private static EdgeDto ToEdgeDto(Edge edge) =>
        new(
            Id: edge.Id,
            SourceNodeId: edge.SourceNodeId,
            TargetNodeId: edge.TargetNodeId,
            Priority: edge.Priority,
            Conditions: edge.ConditionsJson
        );
}