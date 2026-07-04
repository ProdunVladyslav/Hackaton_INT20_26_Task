using Domain.Model.Survey;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Services
{

    /// <summary>
    /// Domain factory service — coordinates multi-entity creation for each node type.
    /// All domain rules for correct construction live here.
    /// Use cases call one method and get back a fully valid, ready-to-persist node.
    /// </summary>
    public sealed class NodeFactory
    {
        private readonly FlowStructureService _structure;
        private readonly IDateTimeProvider _time;

        public NodeFactory(FlowStructureService structure, IDateTimeProvider time)
        {
            _structure = structure;
            _time = time;
        }

        // ── Question ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a Question node with answer type configured.
        /// Validates attribute key uniqueness and reserved key conflicts across the flow.
        /// </summary>
        public Node CreateQuestion(
            Guid flowId,
            IEnumerable<Node> existingFlowNodes,
            string title,
            string attributeKey,
            AnswerType answerType,
            ValueKind valueKind,
            float x,
            float y,
            string? description = null,
            string? mediaUrl = null,
            decimal? sliderMin = null,
            decimal? sliderMax = null)
        {
            _structure.ValidateAttributeKey(
                existingFlowNodes, attributeKey, valueKind, answerType, Guid.Empty);

            var node = Node.Create(flowId, NodeType.Question, title, attributeKey, x, y, _time);

            if (description is not null) node.SetDescription(description);
            if (mediaUrl is not null) node.SetMedia(mediaUrl);

            node.SetAnswerType(answerType, sliderMin, sliderMax, valueKind);

            return node;
        }

        // ── InfoPage ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a pass-through InfoPage node. No answer context produced.
        /// </summary>
        public Node CreateInfoPage(
            Guid flowId,
            string title,
            float x,
            float y,
            string? description = null,
            string? mediaUrl = null)
        {
            var node = Node.Create(flowId, NodeType.InfoPage, title, "", x, y, _time);

            if (description is not null) node.SetDescription(description);
            if (mediaUrl is not null) node.SetMedia(mediaUrl);

            return node;
        }

        // ── LeadCapture ───────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a LeadCapture node with its capture config and fields in one shot.
        /// Rules enforced:
        ///   • Email field must be present.
        ///   • Email field must be marked required.
        ///   • No duplicate field types.
        ///   • Maximum 7 fields.
        /// </summary>
        public Node CreateLeadCapture(
            Guid flowId,
            string title,
            float x,
            float y,
            bool isRequired,
            IEnumerable<LeadCaptureFieldDefinition> fields,
            string? description = null,
            string? mediaUrl = null)
        {
            var fieldList = fields.ToList();

            if (!fieldList.Any(f => f.FieldType == LeadCaptureFieldType.Email))
                throw new DomainException(
                    "LeadCapture node must include an Email field.");

            var node = Node.Create(flowId, NodeType.LeadCapture, title, "", x, y, _time);

            if (description is not null) node.SetDescription(description);
            if (mediaUrl is not null) node.SetMedia(mediaUrl);

            var capture = NodeLeadCapture.Create(node.Id, isRequired);

            // NodeLeadCapture.AddField already enforces uniqueness and max 7 —
            // factory ensures ordering is clean regardless of input order.
            foreach (var (def, index) in fieldList
                .OrderBy(f => f.DisplayOrder)
                .Select((d, i) => (d, i)))
            {
                var field = NodeLeadCaptureField.Create(
                    capture.Id,
                    def.FieldType,
                    def.IsRequired,
                    index,
                    def.Placeholder ?? "");

                capture.AddField(field);
            }

            node.AttachLeadCapture(capture);

            return node;
        }

        // ── Offer ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates an Offer node shell. The Offer entity itself and NodeOffer link
        /// are created separately (existing pattern) because Offer is a standalone
        /// aggregate with its own slug uniqueness check that requires the DB.
        /// </summary>
        public Node CreateOffer(
            Guid flowId,
            string title,
            float x,
            float y,
            string? description = null,
            string? mediaUrl = null)
        {
            var node = Node.Create(flowId, NodeType.Offer, title, "", x, y, _time);

            if (description is not null) node.SetDescription(description);
            if (mediaUrl is not null) node.SetMedia(mediaUrl);

            return node;
        }

        // ── Redirect ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a Redirect node with its config and optional resource links.
        /// Rules enforced:
        ///   • Headline required.
        ///   • Auto-redirect minimum 3 seconds.
        ///   • Maximum 3 resource links.
        ///   • Must end up with a redirect URL or at least one resource link —
        ///     otherwise the respondent has nowhere to go.
        /// </summary>
        public Node CreateRedirect(
            Guid flowId,
            string title,
            string disqualificationReason,
            float x,
            float y,
            string? redirectUrl = null,
            int? autoRedirectAfterSeconds = null,
            IEnumerable<NodeRedirectLinkDefinition>? links = null,
            string? description = null,
            string? mediaUrl = null)
        {
            var node = Node.Create(flowId, NodeType.Redirect, title, "", x, y, _time);

            if (description is not null) node.SetDescription(description);
            if (mediaUrl is not null) node.SetMedia(mediaUrl);

            var redirect = NodeRedirect.Create(node.Id, disqualificationReason);

            if (redirectUrl is not null) redirect.SetRedirectUrl(redirectUrl);
            if (autoRedirectAfterSeconds is not null && autoRedirectAfterSeconds != -1) redirect.SetAutoRedirect(autoRedirectAfterSeconds);

            foreach (var (link, index) in (links ?? [])
                .Select((l, i) => (l, i)))
            {
                redirect.AddLink(NodeRedirectLink.Create(
                    redirect.Id, link.Label, link.Url, index));
            }

            // A Redirect with neither a URL nor any links is a dead end for the
            // respondent — nothing for them to click, nowhere for them to go.
            redirect.EnsureHasDestination();

            node.AttachRedirect(redirect);

            return node;
        }
    }

    // ── Parameter records ─────────────────────────────────────────────────────────
    // Simple data carriers — no logic. Used instead of long parameter lists.

    public record LeadCaptureFieldDefinition(
        LeadCaptureFieldType FieldType,
        bool IsRequired,
        int DisplayOrder = 0,
        string? Placeholder = null);

    public record NodeRedirectLinkDefinition(
        string Label,
        string Url);
}
