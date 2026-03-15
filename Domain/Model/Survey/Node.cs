namespace Domain.Model.Survey
{
    public enum NodeType
    {
        Question,
        InfoPage,
        Offer
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
        Slider
    }

    public sealed class Node
    {
        public Guid Id { get; private set; }
        public Guid FlowId { get; private set; }
        public NodeType Type { get; private set; }
        public string AttributeKey { get; private set; }
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

        private readonly List<Option> _options = new();
        public IReadOnlyCollection<Option> Options => _options.AsReadOnly();

        private Node() { }

        private Node(
            Guid flowId,
            NodeType type,
            string title,
            string attributeKey,
            float positionX,
            float positionY)
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

            CreatedAt = DateTime.UtcNow;
        }

        public static Node Create(
            Guid flowId,
            NodeType type,
            string title,
            string attributeKey,
            float positionX,
            float positionY)
        {
            return new Node(flowId, type, title, attributeKey, positionX, positionY);
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
        public void SetAnswerType(AnswerType? answerType, decimal? sliderMin = null, decimal? sliderMax = null)
        {
            if (answerType.HasValue && Type != NodeType.Question)
                throw new InvalidOperationException("AnswerType can only be set on Question nodes.");

            if (answerType == Survey.AnswerType.Slider)
            {
                if (sliderMin is null || sliderMax is null)
                    throw new ArgumentException("Slider answer type requires both SliderMin and SliderMax.");

                if (sliderMin >= sliderMax)
                    throw new ArgumentException("SliderMin must be less than SliderMax.");

                // Wipe existing options when switching to Slider
                _options.Clear();
            }
            else if (answerType.HasValue)
            {
                // SingleChoice / MultipleChoice
                if (sliderMin is not null || sliderMax is not null)
                    throw new ArgumentException("SliderMin and SliderMax are only valid for the Slider answer type.");
            }

            AnswerType = answerType;
            SliderMin  = answerType == Survey.AnswerType.Slider ? sliderMin : null;
            SliderMax  = answerType == Survey.AnswerType.Slider ? sliderMax : null;
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
    }
}
