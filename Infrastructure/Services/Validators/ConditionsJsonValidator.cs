using Domain.Model.Survey;
using System.Text.Json;

namespace Infrastructure.Services.Validators
{
    public static class ConditionsJsonParser
    {
        private static readonly string[] ValidOperators =
            ["eq", "neq", "in", "gt", "gte", "lt", "lte", "between"];

        /// <summary>
        /// Validates raw JSON shape and returns a parsed ConditionGroup.
        /// Returns null if json is empty (unconditional edge).
        /// Throws ArgumentException if JSON is malformed.
        /// </summary>
        public static ConditionGroup? Parse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            JsonDocument doc;
            try { doc = JsonDocument.Parse(json); }
            catch (JsonException) { throw new ArgumentException("ConditionsJson is not valid JSON."); }

            using (doc)
            {
                return doc.RootElement.ValueKind switch
                {
                    JsonValueKind.Array => ParseArray(doc.RootElement),
                    JsonValueKind.Object => ParseObject(doc.RootElement),
                    _ => throw new ArgumentException("ConditionsJson must be a JSON array or object.")
                };
            }
        }

        private static ConditionGroup ParseArray(JsonElement root)
        {
            var rules = new List<ConditionRule>();
            int i = 0;

            foreach (var element in root.EnumerateArray())
            {
                rules.Add(ParseRule(element, $"[{i}]"));
                i++;
            }

            return new ConditionGroup(rules);
        }

        private static ConditionGroup ParseObject(JsonElement root)
        {
            if (!root.TryGetProperty("operator", out var op))
                throw new ArgumentException("Object format requires an 'operator' field.");

            var logicalOp = op.GetString()?.ToUpperInvariant();
            if (logicalOp is not "AND" and not "OR")
                throw new ArgumentException($"'operator' must be 'AND' or 'OR', got '{op.GetString()}'.");

            if (!root.TryGetProperty("rules", out var rulesEl)
                || rulesEl.ValueKind != JsonValueKind.Array)
                throw new ArgumentException("Object format requires a 'rules' array.");

            var rules = new List<ConditionRule>();
            int i = 0;

            foreach (var element in rulesEl.EnumerateArray())
            {
                rules.Add(ParseRule(element, $"rules[{i}]"));
                i++;
            }

            return new ConditionGroup(rules, logicalOp);
        }

        private static ConditionRule ParseRule(JsonElement rule, string path)
        {
            if (rule.ValueKind != JsonValueKind.Object)
                throw new ArgumentException($"Condition at {path} must be an object.");

            if (!rule.TryGetProperty("AttributeKey", out var key)
                || string.IsNullOrWhiteSpace(key.GetString()))
                throw new ArgumentException($"Condition at {path} is missing 'AttributeKey'.");

            if (!rule.TryGetProperty("Operator", out var operatorProp))
                throw new ArgumentException($"Condition at {path} is missing 'Operator'.");

            var op = operatorProp.GetString()?.ToLowerInvariant();
            if (!ValidOperators.Contains(op))
                throw new ArgumentException(
                    $"Condition at {path} has unsupported operator '{op}'. " +
                    $"Valid: {string.Join(", ", ValidOperators)}.");

            if (!rule.TryGetProperty("Value", out var value))
                throw new ArgumentException($"Condition at {path} is missing 'Value'.");

            if (op == "between" && !rule.TryGetProperty("ValueTo", out _))
                throw new ArgumentException(
                    $"Condition at {path}: operator 'between' requires 'ValueTo'.");

            rule.TryGetProperty("ValueTo", out var valueTo);

            return new ConditionRule(
                key.GetString()!,
                op!,
                value.GetString() ?? "",
                valueTo.ValueKind == JsonValueKind.Undefined ? null : valueTo.GetString());
        }

        /// <summary>
        /// Removes all rules referencing the given attributeKey and
        /// returns the cleaned JSON string. Returns "" if nothing remains.
        /// </summary>
        public static string RemoveConditionsByKey(string? json, string attributeKey)
        {
            var group = Parse(json);
            if (group is null) return "";

            var remaining = group.Rules
                .Where(r => !string.Equals(r.AttributeKey, attributeKey,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (remaining.Count == 0) return "";

            // Serialize back — keep original format as object if it was object
            var cleaned = group with { Rules = remaining };
            return JsonSerializer.Serialize(new
            {
                @operator = cleaned.LogicalOperator,
                rules = cleaned.Rules.Select(r => new
                {
                    AttributeKey = r.AttributeKey,
                    Operator = r.Operator,
                    Value = r.Value,
                    ValueTo = r.ValueTo
                })
            });
        }
    }
}
