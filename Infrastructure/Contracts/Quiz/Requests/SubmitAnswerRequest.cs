using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Contracts.Quiz.Requests;

public sealed record SubmitAnswerRequest([Required] Guid NodeId, string? Value);
