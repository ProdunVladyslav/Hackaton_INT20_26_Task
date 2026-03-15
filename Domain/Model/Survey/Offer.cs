namespace Domain.Model.Survey
{
    public sealed class Offer
    {
        public Guid Id { get; private set; }
        public string Slug { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string Duration { get; private set; }
        public string DigitalContent { get; private set; }
        public string PhysicalWellnessKitName { get; private set; }
        public string PhysicalWellnessKitItems { get; private set; }
        public decimal? Price { get; private set; }
        public string ImageUrl { get; private set; }
        public string CtaText { get; private set; }
        public string CtaUrl { get; private set; }

        private Offer() { }

        private Offer(string slug, string name)
        {
            Id = Guid.NewGuid();
            SetSlug(slug);
            SetName(name);
        }

        public static Offer Create(string slug, string name) => new Offer(slug, name);

        public void SetSlug(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("Slug required");
            Slug = slug;
        }

        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name required");
            Name = name;
        }

        public void SetPrice(decimal price)
        {
            if (price < 0)
                throw new ArgumentException("Price must be positive");
            Price = price;
        }

        public void SetCta(string text, string url)
        {
            CtaText = text;
            CtaUrl = url;
        }

        public void SetDescription(string? description)    => Description = description ?? string.Empty;
        public void SetDuration(string? duration)          => Duration = duration ?? string.Empty;
        public void SetDigitalContent(string? content)     => DigitalContent = content ?? string.Empty;
        public void SetPhysicalWellnessKitName(string? name)  => PhysicalWellnessKitName = name ?? string.Empty;
        public void SetPhysicalWellnessKitItems(string? items) => PhysicalWellnessKitItems = items ?? string.Empty;
        public void SetImageUrl(string? url)               => ImageUrl = url ?? string.Empty;
    }
}
