using Domain.Services;

namespace Domain.Model.Survey
{
    /// <summary>
    /// A trackable source a flow owner shares externally (a specific ad, a
    /// referral partner, a cold-email campaign). Each channel resolves to a
    /// short, globally-unique code embedded in the shareable survey link, so
    /// every session — and downstream Lead — can be attributed back to it.
    /// </summary>
    public sealed class LeadChannel
    {
        public Guid Id { get; private set; }
        public Guid FlowId { get; private set; }
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// 8-character code, unique across ALL flows — the short link has no
        /// flow segment, so the code alone must resolve to one.
        /// </summary>
        public string ShortCode { get; private set; } = string.Empty;

        public bool IsArchived { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private LeadChannel() { }

        public static LeadChannel Create(
            Guid flowId, string name, string shortCode, IDateTimeProvider time)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Channel name is required.");
            if (string.IsNullOrWhiteSpace(shortCode))
                throw new ArgumentException("Short code is required.");

            return new LeadChannel
            {
                Id = Guid.NewGuid(),
                FlowId = flowId,
                Name = name.Trim(),
                ShortCode = shortCode,
                IsArchived = false,
                CreatedAt = time.UtcNow
            };
        }

        public void Rename(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Channel name is required.");
            Name = name.Trim();
        }

        public void Archive() => IsArchived = true;
        public void Restore() => IsArchived = false;
    }
}
