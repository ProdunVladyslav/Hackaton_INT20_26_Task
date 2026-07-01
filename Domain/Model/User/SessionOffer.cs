using Domain.Services;
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
        public DateTime? ConvertedAt { get; private set; }
        public DateTime PresentedAt { get; private set; }

        private SessionOffer() { }

        private SessionOffer(Guid sessionId, Guid offerId, bool primary, DateTime now)
        {
            Id = Guid.NewGuid();
            SessionId = sessionId;
            OfferId = offerId;
            IsPrimary = primary;
            PresentedAt = now;
        }

        public static SessionOffer Create(Guid sessionId, Guid offerId, bool primary, IDateTimeProvider time)
        {
            return new SessionOffer(sessionId, offerId, primary, time.UtcNow);
        }

        public void MarkConverted(IDateTimeProvider time)
        {
            if (Converted) return;
            Converted = true;
            ConvertedAt = time.UtcNow;
        }
    }
}
