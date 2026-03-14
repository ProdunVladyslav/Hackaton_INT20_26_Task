using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Model.User
{
    public class SessionOffer
    {
        public Guid Id { get; private set; }
        public Guid SessionId { get; private set; }
        public Guid OfferId { get; private set; }
        public bool IsPrimary { get; private set; }
        public bool Converted { get; private set; }
        public DateTime PresentedAt { get; private set; }

        private SessionOffer() { }

        private SessionOffer(Guid sessionId, Guid offerId, bool primary)
        {
            Id = Guid.NewGuid();
            SessionId = sessionId;
            OfferId = offerId;
            IsPrimary = primary;
            PresentedAt = DateTime.UtcNow;
        }

        public static SessionOffer Create(Guid sessionId, Guid offerId, bool primary)
        {
            return new SessionOffer(sessionId, offerId, primary);
        }

        public void MarkConverted()
        {
            Converted = true;
        }
    }
}
