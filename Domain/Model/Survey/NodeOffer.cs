using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Model.Survey
{
    public sealed class NodeOffer
    {
        public Guid Id { get; private set; }
        public Guid NodeId { get; private set; }
        public Guid OfferId { get; private set; }

        public Offer Offer { get; private set; } = null!;
        public Node Node { get; private set; } = null!;


        public bool IsPrimary { get; private set; }

        private NodeOffer() { }

        private NodeOffer(Guid nodeId, Guid offerId, bool isPrimary)
        {
            Id = Guid.NewGuid();
            NodeId = nodeId;
            OfferId = offerId;
            IsPrimary = isPrimary;
        }

        public static NodeOffer Create(Guid nodeId, Guid offerId, bool isPrimary)
        {
            return new NodeOffer(nodeId, offerId, isPrimary);
        }

        public void SetPrimary(bool value)
        {
            IsPrimary = value;
        }
    }
}
