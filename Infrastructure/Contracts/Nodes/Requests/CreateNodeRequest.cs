using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Contracts.Nodes.Requests;

public sealed record CreateNodeRequest(
    [Required] string Type,
    [Required][MaxLength(500)] string Title,
    string? AttributeKey,
    string? Description,
    string? MediaUrl,
    float PositionX = 0f,
    float PositionY = 0f
);
