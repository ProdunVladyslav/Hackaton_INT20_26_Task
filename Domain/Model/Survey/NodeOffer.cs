namespace Domain.Model.Survey
{
    public enum CalendarProvider
    {
        Calendly,
        CalCom,
        HubSpot,
        Custom,
        None
    }

    public sealed class NodeOffer
    {
        public Guid Id { get; private set; }
        public Guid NodeId { get; private set; }
        public Guid OfferId { get; private set; }

        public Offer Offer { get; private set; } = null!;
        public Node Node { get; private set; } = null!;

        // NEW: qualification context for this specific node→offer link
        public QualificationTier Tier { get; private set; }
        public CalendarProvider? CalendarProvider { get; private set; }

        // NEW: which sales rep/owner gets notified when this offer is reached
        // null = notify flow owner, set = notify specific user
        public Guid? AssignedOwnerId { get; private set; }

        public bool IsPrimary { get; private set; }

        private NodeOffer() { }

        private NodeOffer(Guid nodeId, Guid offerId, bool isPrimary)
        {
            Id = Guid.NewGuid();
            NodeId = nodeId;
            OfferId = offerId;
            IsPrimary = isPrimary;
            Tier = QualificationTier.Hot;
        }

        public static NodeOffer Create(Guid nodeId, Guid offerId, bool isPrimary)
        {
            return new NodeOffer(nodeId, offerId, isPrimary);
        }

        public void SetPrimary(bool value)
        {
            IsPrimary = value;
        }

        public void SetTier(QualificationTier tier) => Tier = tier;
        public void SetCalendarProvider(CalendarProvider? provider) => CalendarProvider = provider;
        public void SetAssignedOwner(Guid? ownerId) => AssignedOwnerId = ownerId;
    }
}
