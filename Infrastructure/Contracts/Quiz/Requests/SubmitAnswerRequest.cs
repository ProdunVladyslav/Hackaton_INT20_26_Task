using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Contracts.Quiz.Requests;

public sealed record SubmitAnswerRequest(
    Guid NodeId,
    string? Value = null,
    // Populated when submitting a LeadCapture node.
    // Key = AttributeKey (e.g. "lead_email"), Value = user input.
    Dictionary<string, string>? LeadFields = null
);
