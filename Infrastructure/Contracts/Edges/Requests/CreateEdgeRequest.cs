using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Contracts.Edges.Requests;

public sealed record CreateEdgeRequest(
    [Required] Guid SourceNodeId,
    [Required] Guid TargetNodeId,
    int Priority = 0,
    string? ConditionsJson = null
);
