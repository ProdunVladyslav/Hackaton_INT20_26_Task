using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Contracts.Options.Requests;

public sealed record CreateOptionRequest(
    [Required] string Label,
    [Required] string Value,
    int DisplayOrder = 0,
    string? MediaUrl = null
);
