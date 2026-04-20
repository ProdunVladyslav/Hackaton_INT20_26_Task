namespace Domain.Model.Survey;

/// <summary>
/// A single condition rule extracted from an edge's condition JSON.
/// Pure value object — no JSON knowledge.
/// </summary>
public sealed record ConditionRule(
    string AttributeKey,
    string Operator,
    string Value,
    string? ValueTo = null);
