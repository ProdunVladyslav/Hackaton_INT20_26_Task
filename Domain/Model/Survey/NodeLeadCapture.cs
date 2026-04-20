namespace Domain.Model.Survey
{
    public sealed class NodeLeadCapture
    {
        public Guid Id { get; private set; }
        public Guid NodeId { get; private set; }

        // Gate: if true, user cannot proceed without submitting.
        // If false, there's a "Skip" option shown.
        public bool IsRequired { get; private set; }

        private readonly List<NodeLeadCaptureField> _fields = new();
        public IReadOnlyCollection<NodeLeadCaptureField> Fields => _fields.AsReadOnly();

        private NodeLeadCapture() { }

        public static NodeLeadCapture Create(Guid nodeId, bool isRequired = true)
        {
            return new NodeLeadCapture
            {
                Id = Guid.NewGuid(),
                NodeId = nodeId,
                IsRequired = isRequired
            };
        }

        public void AddField(NodeLeadCaptureField field)
        {
            if (_fields.Any(f => f.FieldType == field.FieldType))
                throw new InvalidOperationException($"Field {field.FieldType} already added.");

            if (_fields.Count >= 7)
                throw new InvalidOperationException("Maximum 7 fields per LeadCapture node.");

            _fields.Add(field);
        }

        public void SetRequired(bool value) => IsRequired = value;
    }
}
