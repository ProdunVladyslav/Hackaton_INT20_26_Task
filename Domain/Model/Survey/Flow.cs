using Domain.Model.AdminProfile;
using Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Domain.Model.Survey
{
    public sealed class Flow
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public bool IsPublished { get; private set; }
        public Guid? EntryNodeId { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }
        public Guid OwnerId { get; private set; }
        public UserProfile Owner { get; private set; }

        private readonly List<Node> _nodes = new();
        public IReadOnlyCollection<Node> Nodes => _nodes.AsReadOnly();

        private readonly List<Edge> _edges = new();
        public IReadOnlyCollection<Edge> Edges => _edges.AsReadOnly();

        private Flow() { }

        private Flow(string name, string description, Guid ownerId, DateTime now)
        {
            Id = Guid.NewGuid();
            OwnerId = ownerId;
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Flow name cannot be empty");
            Name = name;
            Description = description;
            CreatedAt = now;
            UpdatedAt = now;
        }

        public static Flow Create(string name, string description, Guid ownerId, IDateTimeProvider time)
        {
            if (ownerId == Guid.Empty)
                throw new ArgumentException("Owner is required");

            return new Flow(name, description, ownerId, time.UtcNow);
        }

        public void SetName(string name, IDateTimeProvider time)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Flow name cannot be empty");

            Name = name;
            Touch(time.UtcNow);
        }

        public void SetDescription(string description, IDateTimeProvider time)
        {
            Description = description;
            Touch(time.UtcNow);
        }

        public void SetEntryNode(Guid nodeId, IDateTimeProvider time)
        {
            if (_nodes.All(n => n.Id != nodeId))
                throw new InvalidOperationException("Entry node must belong to this flow");

            EntryNodeId = nodeId;
            Touch(time.UtcNow);
        }

        public void UnsetEntryNodeId(IDateTimeProvider time)
        {
            EntryNodeId = null;
            Touch(time.UtcNow);
        }

        public void Publish(IDateTimeProvider time)
        {
            if (EntryNodeId == null)
                throw new InvalidOperationException("Cannot publish flow without entry node");

            IsPublished = true;
            Touch(time.UtcNow);
        }

        public void Unpublish(IDateTimeProvider time)
        {
            IsPublished = false;
            Touch(time.UtcNow);
        }

        public void AddNode(Node node)
        {
            if (node.FlowId != Id)
                throw new InvalidOperationException("Node belongs to different flow");

            _nodes.Add(node);
        }

        public void AddEdge(Edge edge)
        {
            if (edge.FlowId != Id)
                throw new InvalidOperationException("Edge belongs to different flow");

            _edges.Add(edge);
        }

        private void Touch(DateTime now)
        {
            UpdatedAt = now;
        }
    }
}
