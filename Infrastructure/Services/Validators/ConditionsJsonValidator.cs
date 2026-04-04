using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Services.Validators
{
    internal static class ConditionsJsonValidator
    {
        private static readonly string[] ValidOperators = ["eq", "neq", "in", "gt", "gte", "lt", "lte", "between"];

        public static string? Validate(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null; // empty is valid — unconditional edge

            JsonDocument doc;
            try { doc = JsonDocument.Parse(json); }
            catch (JsonException) { return "ConditionsJson is not valid JSON."; }

            using (doc)
            {
                return doc.RootElement.ValueKind switch
                {
                    JsonValueKind.Array => ValidateArray(doc.RootElement),
                    JsonValueKind.Object => ValidateObject(doc.RootElement),
                    _ => "ConditionsJson must be a JSON array or object."
                };
            }
        }

        // ── Format 1: [ { AttributeKey, Operator, Value, ?ValueTo } ] ────────────

        private static string? ValidateArray(JsonElement root)
        {
            int i = 0;
            foreach (var rule in root.EnumerateArray())
            {
                var err = ValidateRule(rule, $"[{i}]");
                if (err is not null) return err;
                i++;
            }
            return null;
        }

        // ── Format 2: { operator: "AND"|"OR", rules: [ ... ] } ───────────────────

        private static string? ValidateObject(JsonElement root)
        {
            if (!root.TryGetProperty("operator", out var op))
                return "Object format requires an 'operator' field.";

            var opValue = op.GetString()?.ToUpperInvariant();
            if (opValue is not "AND" and not "OR")
                return $"'operator' must be 'AND' or 'OR', got '{op.GetString()}'.";

            if (!root.TryGetProperty("rules", out var rules) || rules.ValueKind != JsonValueKind.Array)
                return "Object format requires a 'rules' array.";

            int i = 0;
            foreach (var rule in rules.EnumerateArray())
            {
                var err = ValidateRule(rule, $"rules[{i}]");
                if (err is not null) return err;
                i++;
            }

            return null;
        }

        // ── Shared rule validation ────────────────────────────────────────────────

        private static string? ValidateRule(JsonElement rule, string path)
        {
            if (rule.ValueKind != JsonValueKind.Object)
                return $"Condition at {path} must be an object.";

            if (!rule.TryGetProperty("AttributeKey", out var key) || string.IsNullOrWhiteSpace(key.GetString()))
                return $"Condition at {path} is missing 'AttributeKey'.";

            if (!rule.TryGetProperty("Operator", out var operatorProp))
                return $"Condition at {path} is missing 'Operator'.";

            var op = operatorProp.GetString()?.ToLowerInvariant();
            if (!ValidOperators.Contains(op))
                return $"Condition at {path} has unsupported operator '{op}'. Valid: {string.Join(", ", ValidOperators)}.";

            if (!rule.TryGetProperty("Value", out _))
                return $"Condition at {path} is missing 'Value'.";

            // 'between' requires ValueTo
            if (op == "between" && !rule.TryGetProperty("ValueTo", out _))
                return $"Condition at {path}: operator 'between' requires 'ValueTo'.";

            return null;
        }

        public static IReadOnlyDictionary<string, string> GetAttributeKeys(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, string>();

            JsonDocument doc;
            try { doc = JsonDocument.Parse(json); }
            catch (JsonException) { return new Dictionary<string, string>(); }

            using (doc)
            {
                return doc.RootElement.ValueKind switch
                {
                    JsonValueKind.Array => ExtractKeysFromArray(doc.RootElement),
                    JsonValueKind.Object => ExtractKeysFromObject(doc.RootElement),
                    _ => new Dictionary<string, string>()
                };
            }
        }

        private static IReadOnlyDictionary<string, string> ExtractKeysFromArray(JsonElement root)
            => root.EnumerateArray()
                   .Where(r => r.TryGetProperty("AttributeKey", out var k) && !string.IsNullOrWhiteSpace(k.GetString())
                            && r.TryGetProperty("Operator", out _))
                   .GroupBy(
                       r => r.GetProperty("AttributeKey").GetString()!,
                       r => r.GetProperty("Operator").GetString()!,
                       StringComparer.OrdinalIgnoreCase)
                   .ToDictionary(
                       g => g.Key,
                       g => string.Join(",", g.Distinct(StringComparer.OrdinalIgnoreCase)), // e.g. "eq,gt" if same key used twice
                       StringComparer.OrdinalIgnoreCase);

        private static IReadOnlyDictionary<string, string> ExtractKeysFromObject(JsonElement root)
        {
            if (!root.TryGetProperty("rules", out var rules) || rules.ValueKind != JsonValueKind.Array)
                return new Dictionary<string, string>();

            return ExtractKeysFromArray(rules);
        }

        /// <summary>
        /// Removes all conditions referencing the given attributeKey.
        /// Returns "" if no conditions remain, otherwise returns the cleaned JSON.
        /// </summary>
        public static string RemoveConditionsByKey(string? json, string attributeKey)
        {
            if (string.IsNullOrWhiteSpace(json)) return "";

            JsonDocument doc;
            try { doc = JsonDocument.Parse(json); }
            catch (JsonException) { return ""; }

            using (doc)
            {
                return doc.RootElement.ValueKind switch
                {
                    JsonValueKind.Array => RemoveFromArray(doc.RootElement, attributeKey),
                    JsonValueKind.Object => RemoveFromObject(doc.RootElement, attributeKey),
                    _ => ""
                };
            }
        }

        private static string RemoveFromArray(JsonElement root, string attributeKey)
        {
            var remaining = root.EnumerateArray()
                .Where(r => !r.TryGetProperty("AttributeKey", out var k) ||
                            !string.Equals(k.GetString(), attributeKey, StringComparison.OrdinalIgnoreCase))
                .Select(r => r.Clone())
                .ToList();

            return remaining.Count == 0
                ? ""
                : JsonSerializer.Serialize(remaining);
        }

        private static string RemoveFromObject(JsonElement root, string attributeKey)
        {
            if (!root.TryGetProperty("rules", out var rules) || rules.ValueKind != JsonValueKind.Array)
                return "";

            var remaining = rules.EnumerateArray()
                .Where(r => !r.TryGetProperty("AttributeKey", out var k) ||
                            !string.Equals(k.GetString(), attributeKey, StringComparison.OrdinalIgnoreCase))
                .Select(r => r.Clone())
                .ToList();

            if (remaining.Count == 0) return "";

            // Reconstruct the wrapper object preserving the operator
            var op = root.TryGetProperty("operator", out var opProp) ? opProp.GetString() : "AND";
            var result = new { @operator = op, rules = remaining };
            return JsonSerializer.Serialize(result);
        }
    }
}
