namespace Domain.Model.Survey
{
    /// <summary>
    /// Represents the sales outcome shown to a qualified lead.
    /// In the B2B qualification context this is primarily a booking/CTA screen —
    /// not a product listing. Fields reflect that purpose.
    /// </summary>
    public sealed class Offer
    {
        public Guid Id { get; private set; }
        public Guid OwnerId { get; private set; }

        // ── Identity ──────────────────────────────────────────────────────────
        // Slug is the public URL-safe identifier.
        // Name is internal — shown in the builder, not to the lead.
        public string Slug { get; private set; }
        public string Name { get; private set; }

        // ── Lead-facing content ───────────────────────────────────────────────
        // Headline supports {{token}} substitution (e.g. {{lead_company}}).
        public string Headline { get; private set; }
        public string Body { get; private set; }
        public string? ImageUrl { get; private set; }

        // ── Call to action ────────────────────────────────────────────────────
        public string CtaText { get; private set; }
        public string CtaUrl { get; private set; }

        // ── Calendar booking (optional) ───────────────────────────────────────
        // When set, renders an embedded/redirect booking experience.
        // CalendarUrl is the raw link — provider tells the frontend how to render it.
        public string? CalendarUrl { get; private set; }

        private Offer() { }

        private Offer(string slug, string name, Guid ownerId)
        {
            Id = Guid.NewGuid();
            OwnerId = ownerId;
            SetSlug(slug);
            SetName(name);
            Headline = string.Empty;
            Body = string.Empty;
            CtaText = string.Empty;
            CtaUrl = string.Empty;
        }

        public static Offer Create(string slug, string name, Guid ownerId)
            => new(slug, name, ownerId);

        public void SetSlug(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("Slug required.");
            Slug = slug;
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name required.");
            Name = name;
        }

        /// <summary>
        /// Headline supports {{lead_name}}, {{lead_company}} token substitution.
        /// </summary>
        public void SetHeadline(string headline) => Headline = headline ?? string.Empty;
        public void SetBody(string? body) => Body = body ?? string.Empty;
        public void SetImageUrl(string? url) => ImageUrl = url;

        public void SetCta(string text, string url)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("CTA text required.");
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("CTA url required.");
            CtaText = text;
            CtaUrl = url;
        }

        public void SetCalendarUrl(string? url) => CalendarUrl = url;
    }
}