using Domain.Model.AdminProfile;
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

        private Flow(string name, string description, Guid ownerId)
        {
            Id = Guid.NewGuid();
            OwnerId = ownerId;
            SetName(name);
            Description = description;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public static Flow Create(string name, string description, Guid ownerId)
        {
            if (ownerId == Guid.Empty)
                throw new ArgumentException("Owner is required");

            return new Flow(name, description, ownerId);
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Flow name cannot be empty");

            Name = name;
            Touch();
        }

        public void SetDescription(string description)
        {
            Description = description;
            Touch();
        }

        public void SetEntryNode(Guid nodeId)
        {
            if (_nodes.All(n => n.Id != nodeId))
                throw new InvalidOperationException("Entry node must belong to this flow");

            EntryNodeId = nodeId;
            Touch();
        }

        public void Publish()
        {
            if (EntryNodeId == null)
                throw new InvalidOperationException("Cannot publish flow without entry node");

            IsPublished = true;
            Touch();
        }

        public void Unpublish()
        {
            IsPublished = false;
            Touch();
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

        private void Touch()
        {
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
