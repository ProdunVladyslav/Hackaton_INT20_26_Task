using Domain.Services;

namespace Domain.Model.Survey
{
    public enum NodeType
    {
        Question,
        InfoPage,
        Offer,
        LeadCapture,
        Redirect
    }

    public enum ValueKind
    {
        Text,      // eq, neq, in
        Numeric,   // eq, neq, in, gt, gte, lt, lte, between
    }

    public enum AnswerTypesSwitchableInternally
    {
        Choice,
        Slider,
        Text
    }

    /// <summary>
    /// How a Question node collects input from the user.
    /// Only valid on Question nodes.
    /// </summary>
    public enum AnswerType
    {
        /// <summary>User picks exactly one option from the list.</summary>
        SingleChoice,

        /// <summary>User picks one or more options from the list.</summary>
        MultipleChoice,

        /// <summary>
        /// User drags a numeric slider between SliderMin and SliderMax.
        /// Options are not allowed on Slider questions.
        /// </summary>
        Slider,
        Text
    }

    public sealed class Node
    {
        public Guid Id { get; private set; }
        public Guid FlowId { get; private set; }
        public NodeType Type { get; private set; }
        public string AttributeKey { get; private set; }
        public ValueKind? ValueKind { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string MediaUrl { get; private set; }
        public float PositionX { get; private set; }
        public float PositionY { get; private set; }
        public DateTime CreatedAt { get; private set; }

        // ── Answer type (Question nodes only) ────────────────────────────────
        public AnswerType? AnswerType { get; private set; }
        public decimal? SliderMin { get; private set; }
        public decimal? SliderMax { get; private set; }
        public NodeLeadCapture? LeadCapture { get; private set; }
        public NodeRedirect? Redirect { get; private set; }

        private readonly List<Option> _options = new();
        public IReadOnlyCollection<Option> Options => _options.AsReadOnly();

        private Node() { }

        private Node(
            Guid flowId,
            NodeType type,
            string title,
            string attributeKey,
            float positionX,
            float positionY,
            DateTime now)
        {
            Id = Guid.NewGuid();
            FlowId = flowId;
            Type = type;

            SetTitle(title);
            SetAttributeKey(attributeKey);

            Description = "";
            MediaUrl = "";
            PositionX = positionX;
            PositionY = positionY;

            CreatedAt = now;
        }

        public static Node Create(
            Guid flowId,
            NodeType type,
            string title,
            string attributeKey,
            float positionX,
            float positionY,
            IDateTimeProvider time)
        {
            return new Node(flowId, type, title, attributeKey, positionX, positionY, time.UtcNow);
        }

        public void SetTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Node title cannot be empty");

            Title = title;
        }

        public void SetDescription(string description)
        {
            Description = description;
        }

        public void SetMedia(string url)
        {
            MediaUrl = url;
        }

        public void SetAttributeKey(string key)
        {
            if (Type == NodeType.Question && string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("Question node must have attribute key");

            AttributeKey = key;
        }

        /// <summary>
        /// Sets the answer type for a Question node.
        ///
        /// Rules:
        ///   • Only Question nodes may have an AnswerType.
        ///   • Slider requires sliderMin &lt; sliderMax and no existing options.
        ///   • SingleChoice / MultipleChoice must not supply slider bounds.
        ///   • Pass null to clear the answer type (resets slider bounds too).
        /// </summary>
        public void SetAnswerType(
            AnswerType? answerType,
            decimal? sliderMin = null,
            decimal? sliderMax = null,
            ValueKind? valueKind = null)
        {
            if (answerType.HasValue && Type != NodeType.Question)
                throw new InvalidOperationException("AnswerType can only be set on Question nodes.");

            if (answerType == Survey.AnswerType.Slider)
            {
                if (sliderMin is null || sliderMax is null)
                    throw new ArgumentException("Slider answer type requires both SliderMin and SliderMax.");

                if (sliderMin >= sliderMax)
                    throw new ArgumentException("SliderMin must be less than SliderMax.");

                if (valueKind == Survey.ValueKind.Text)
                    throw new ArgumentException("Slider answer type cannot have Text ValueKind.");

                _options.Clear();

                AnswerType = answerType;
                SliderMin = sliderMin;
                SliderMax = sliderMax;
                ValueKind = valueKind ?? Survey.ValueKind.Numeric;
            }
            else if (answerType.HasValue)
            {
                if (sliderMin is not null || sliderMax is not null)
                    throw new ArgumentException("SliderMin and SliderMax are only valid for the Slider answer type.");

                AnswerType = answerType;
                SliderMin = null;
                SliderMax = null;
                ValueKind = valueKind ?? Survey.ValueKind.Text;
            }
            else
            {
                // clearing
                AnswerType = null;
                SliderMin = null;
                SliderMax = null;
                ValueKind = null;
            }
        }

        public void Move(float x, float y)
        {
            PositionX = x;
            PositionY = y;
        }

        public void AddOption(Option option)
        {
            if (Type != NodeType.Question)
                throw new InvalidOperationException("Only question nodes can have options.");

            if (AnswerType == Survey.AnswerType.Slider)
                throw new InvalidOperationException("Slider questions cannot have options.");

            _options.Add(option);
        }

        /// <summary>
        /// Attaches a LeadCapture config to this node.
        /// Rules:
        ///   - Only LeadCapture nodes may have this config.
        ///   - Can only be attached once — reassignment is not allowed.
        ///   - The config must belong to this node (NodeId must match).
        /// </summary>
        public void AttachLeadCapture(NodeLeadCapture leadCapture)
        {
            if (Type != NodeType.LeadCapture)
                throw new InvalidOperationException(
                    $"LeadCapture config can only be attached to LeadCapture nodes. This node is '{Type}'.");

            if (LeadCapture is not null)
                throw new InvalidOperationException(
                    "A LeadCapture config is already attached to this node.");

            if (leadCapture.NodeId != Id)
                throw new ArgumentException(
                    "The LeadCapture config's NodeId does not match this node's Id.");

            LeadCapture = leadCapture;
        }

        /// <summary>
        /// Attaches a Redirect config to this node.
        /// Rules:
        ///   - Only Redirect nodes may have this config.
        ///   - Can only be attached once — reassignment is not allowed.
        ///   - The config must belong to this node (NodeId must match).
        /// </summary>
        public void AttachRedirect(NodeRedirect redirect)
        {
            if (Type != NodeType.Redirect)
                throw new InvalidOperationException(
                    $"Redirect config can only be attached to Redirect nodes. This node is '{Type}'.");

            if (Redirect is not null)
                throw new InvalidOperationException(
                    "A Redirect config is already attached to this node.");

            if (redirect.NodeId != Id)
                throw new ArgumentException(
                    "The Redirect config's NodeId does not match this node's Id.");

            Redirect = redirect;
        }
    }
}
