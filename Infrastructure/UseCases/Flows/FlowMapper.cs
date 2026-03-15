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
    /// <param name="flow">The flow domain entity.</param>
    /// <param name="stats">Optional admin statistics; pass null for public endpoints.</param>
    public static FlowSummaryResponse ToSummary(Flow flow, FlowAdminStats? stats = null) =>
        new(
            Id          : flow.Id,
            Name        : flow.Name,
            Description : flow.Description,
            IsPublished : flow.IsPublished,
            EntryNodeId : flow.EntryNodeId,
            CreatedAt   : flow.CreatedAt,
            UpdatedAt   : flow.UpdatedAt,
            Stats       : stats
        );

    /// <summary>Maps a Flow (with fully loaded DAG) to the detailed response.</summary>
    /// <param name="flow">The flow domain entity with Nodes and Edges loaded.</param>
    /// <param name="stats">Optional flow-level admin stats; pass null for public endpoints.</param>
    public static FlowDetailResponse ToDetail(Flow flow, FlowAdminStats? stats = null) =>
        new(
            Id          : flow.Id,
            Name        : flow.Name,
            Description : flow.Description,
            IsPublished : flow.IsPublished,
            EntryNodeId : flow.EntryNodeId,
            CreatedAt   : flow.CreatedAt,
            UpdatedAt   : flow.UpdatedAt,
            Nodes       : flow.Nodes.Select(ToNodeDto).ToList(),
            Edges       : flow.Edges.Select(ToEdgeDto).ToList(),
            Stats       : stats
        );

    private static NodeDto ToNodeDto(Node node) =>
        new(
            Id           : node.Id,
            Type         : node.Type.ToString(),
            AttributeKey : node.AttributeKey,
            Title        : node.Title,
            Description  : node.Description,
            MediaUrl     : node.MediaUrl,
            PositionX    : node.PositionX,
            PositionY    : node.PositionY,
            CreatedAt    : node.CreatedAt,
            Options      : node.Options.Select(ToOptionDto).ToList(),
            NodeOffers   : new List<NodeOfferDto>(), // enriched separately in use case
            Stats        : null                       // enriched separately in use case
        );

    private static OptionDto ToOptionDto(Option opt) =>
        new(
            Id           : opt.Id,
            Label        : opt.Label,
            Value        : opt.Value,
            DisplayOrder : opt.DisplayOrder,
            MediaUrl     : opt.MediaUrl
        );

    private static EdgeDto ToEdgeDto(Edge edge) =>
        new(
            Id           : edge.Id,
            SourceNodeId : edge.SourceNodeId,
            TargetNodeId : edge.TargetNodeId,
            Priority     : edge.Priority,
            Conditions   : edge.ConditionsJson
        );
}
