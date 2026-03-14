using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Contracts.Flows.Requests;

public sealed record CreateFlowRequest(
    [Required(ErrorMessage = "Flow name is required.")]
    [MaxLength(200)]
    string Name,

    string? Description
);
