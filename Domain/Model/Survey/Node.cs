using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Model.Survey
{
    public enum NodeType
    {
        Question,
        InfoPage,
        Offer
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

        public void Move(float x, float y)
        {
            PositionX = x;
            PositionY = y;
        }

        public void AddOption(Option option)
        {
            if (Type != NodeType.Question)
                throw new InvalidOperationException("Only question nodes can have options");

            _options.Add(option);
        }
    }
}
