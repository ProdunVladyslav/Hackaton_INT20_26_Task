using Domain.Model.Survey;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Services
{
    /// <summary>
    /// Pure domain service — no infrastructure dependencies.
    /// Owns cross-entity validation that cannot live inside a single entity.
    /// </summary>
    public sealed class FlowStructureService
    {
        private static readonly HashSet<string> _reservedAttributeKeys =
            new(StringComparer.OrdinalIgnoreCase)
            {
            "__score__",
            "lead_email",
            "lead_name",
            "lead_phone",
            "lead_company",
            "lead_title",
            "lead_company_size",
            "lead_website"
            };

        // ── Attribute key rules ───────────────────────────────────────────────────

        /// <summary>
        /// Throws if the proposed key is reserved by the system or already
        /// used with a conflicting ValueKind in the same flow.
        /// Pass excludeNodeId = Guid.Empty on create, or the node's own Id on update.
        /// </summary>
        public void ValidateAttributeKey(
            IEnumerable<Node> flowNodes,
            string attributeKey,
            ValueKind proposedValueKind,
            Guid excludeNodeId)
        {
            if (_reservedAttributeKeys.Contains(attributeKey))
                throw new DomainException(
                    $"'{attributeKey}' is reserved by the system and cannot be used as an attribute key.");

            var conflict = flowNodes
                .Where(n =>
                    n.Id != excludeNodeId &&
                    n.Type == NodeType.Question &&
                    string.Equals(n.AttributeKey, attributeKey, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault(n => n.ValueKind != proposedValueKind);

            if (conflict is not null)
                throw new DomainException(
                    $"Attribute key '{attributeKey}' is already used with ValueKind " +
                    $"'{conflict.ValueKind}' in this flow. All nodes sharing a key must have the same ValueKind.");
        }

        // ── Edge / DAG rules ──────────────────────────────────────────────────────

        /// <summary>
        /// Throws if adding an edge from source → target would create a cycle.
        /// </summary>
        public void ValidateNoCycle(IEnumerable<Edge> existingEdges, Guid sourceNodeId, Guid targetNodeId)
        {
            // If we can walk from target back to source using existing edges, it's a cycle.
            var edges = existingEdges.ToList();
            var visited = new HashSet<Guid>();
            var queue = new Queue<Guid>();
            queue.Enqueue(targetNodeId);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current == sourceNodeId)
                    throw new DomainException(
                        "Adding this edge would create a cycle in the flow.");

                if (!visited.Add(current)) continue;

                foreach (var edge in edges.Where(e => e.SourceNodeId == current))
                    queue.Enqueue(edge.TargetNodeId);
            }
        }

        /// <summary>
        /// Throws if the source node type is not allowed to have outgoing edges.
        /// Terminal nodes (Offer, Redirect) must be leaf nodes.
        /// </summary>
        public void ValidateEdgeSource(Node sourceNode)
        {
            if (sourceNode.Type is NodeType.Offer or NodeType.Redirect)
                throw new DomainException(
                    $"{sourceNode.Type} nodes are terminal and cannot have outgoing edges.");
        }

        /// <summary>
        /// Throws if a pass-through node (InfoPage, LeadCapture) is given a conditional edge.
        /// These nodes do not produce answer context so conditions are meaningless.
        /// </summary>
        public void ValidateEdgeConditions(Node sourceNode, string? conditionsJson)
        {
            if (sourceNode.Type is NodeType.InfoPage or NodeType.LeadCapture
                && !string.IsNullOrWhiteSpace(conditionsJson))
                throw new DomainException(
                    $"{sourceNode.Type} nodes do not support conditional edges. " +
                    "Use an unconditional edge (empty conditions).");
        }

        /// <summary>
        /// Validates that every rule in the condition group references a real
        /// attribute key in this flow, and uses operators compatible with its ValueKind.
        /// </summary>
        private static readonly string[] _numericOperators =
            ["eq", "neq", "in", "gt", "gte", "lt", "lte", "between"];

        public void ValidateConditionGroup(
            ConditionGroup conditions,
            IEnumerable<Node> flowNodes)
        {
            foreach (var rule in conditions.Rules)
            {
                // __score__ is a built-in numeric attribute injected into every edge evaluation context.
                // It accumulates ScoreDelta from all answers submitted so far in the session.
                if (string.Equals(rule.AttributeKey, "__score__", StringComparison.OrdinalIgnoreCase))
                {
                    if (!_numericOperators.Contains(rule.Operator, StringComparer.OrdinalIgnoreCase))
                        throw new DomainException(
                            $"'__score__' is a built-in numeric attribute. " +
                            $"Operator '{rule.Operator}' is not allowed. " +
                            $"Allowed: {string.Join(", ", _numericOperators)}.");
                    continue;
                }

                var node = flowNodes.FirstOrDefault(n =>
                    string.Equals(n.AttributeKey, rule.AttributeKey,
                        StringComparison.OrdinalIgnoreCase));

                if (node is null)
                    throw new DomainException(
                        $"AttributeKey '{rule.AttributeKey}' is not defined by any node in this flow.");

                var allowedOperators = node.ValueKind switch
                {
                    ValueKind.Text => new[] { "eq", "neq", "in" },
                    ValueKind.Numeric => new[] { "eq", "neq", "in", "gt", "gte", "lt", "lte", "between" },
                    _ => new[] { "eq", "neq" }
                };

                if (!allowedOperators.Contains(rule.Operator, StringComparer.OrdinalIgnoreCase))
                    throw new DomainException(
                        $"AttributeKey '{rule.AttributeKey}' has ValueKind '{node.ValueKind}' " +
                        $"which does not support operator '{rule.Operator}'. " +
                        $"Allowed: {string.Join(", ", allowedOperators)}.");
            }
        }

        // ValidateEdge now receives a ConditionGroup instead of raw json string
        public void ValidateEdge(
            Node sourceNode,
            Node targetNode,
            ConditionGroup? conditions,
            IEnumerable<Edge> existingEdges,
            IEnumerable<Node> flowNodes)
        {
            if (sourceNode.FlowId != targetNode.FlowId)
                throw new DomainException(
                    "Source and target nodes must belong to the same flow.");

            if (sourceNode.Type is NodeType.Offer or NodeType.Redirect)
                throw new DomainException(
                    $"{sourceNode.Type} nodes are terminal and cannot have outgoing edges.");

            if (sourceNode.Type is NodeType.InfoPage or NodeType.LeadCapture
                && conditions is { Rules.Count: > 0 })
                throw new DomainException(
                    $"{sourceNode.Type} nodes do not support conditional edges.");

            ValidateNoCycle(existingEdges, sourceNode.Id, targetNode.Id);

            if (conditions is { Rules.Count: > 0 })
                ValidateConditionGroup(conditions, flowNodes);
        }
    }
}
