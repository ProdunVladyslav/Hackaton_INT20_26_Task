using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Contracts.AIGeneration.Requests
{
    public sealed record GenerateFlowRequest(
        [Required]
        [MinLength(10, ErrorMessage = "Prompt is too short to generate a meaningful flow.")]
        string UserPrompt
    );
}
