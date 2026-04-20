namespace Domain.Model.Survey;

/// <summary>
/// Parsed representation of an edge's condition block.
/// Either a flat list of rules (array format) or
/// an AND/OR group (object format).
/// </summary>
public sealed record ConditionGroup(
    IReadOnlyList<ConditionRule> Rules,
    string LogicalOperator = "AND");  // "AND" | "OR"