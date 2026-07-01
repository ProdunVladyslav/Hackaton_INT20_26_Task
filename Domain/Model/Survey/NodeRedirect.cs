namespace Domain.Model.Survey
{
    public enum QualificationTier
    {
        Hot,
        Warm,
        Cold,
        Disqualified
    }

    public sealed class NodeRedirect
    {
        public Guid Id { get; private set; }
        public Guid NodeId { get; private set; }

        // Where to send user — blog post, free tier, self-serve, etc.
        public string? RedirectUrl { get; private set; }

        // How long before auto-redirect fires (null = no auto-redirect)
        public int? AutoRedirectAfterSeconds { get; private set; }

        // What tier this path represents — used for analytics + lead table
        public string DisqualificationReason { get; private set; }

        private readonly List<NodeRedirectLink> _links = new();
        public IReadOnlyCollection<NodeRedirectLink> Links => _links.AsReadOnly();

        private NodeRedirect() { }

        public static NodeRedirect Create(Guid nodeId, string disqualificationReason)
        {
            return new NodeRedirect
            {
                Id = Guid.NewGuid(),
                NodeId = nodeId,
                DisqualificationReason = disqualificationReason
            };
        }

        public void SetRedirectUrl(string? url) => RedirectUrl = url;
        public void SetAutoRedirect(int? seconds)
        {
            if (seconds is < 3)
                throw new ArgumentException("Auto-redirect minimum is 3 seconds.");
            AutoRedirectAfterSeconds = seconds;
        }

        public void AddLink(NodeRedirectLink link)
        {
            if (_links.Count >= 3)
                throw new InvalidOperationException("Maximum 3 resource links per Redirect node.");
            _links.Add(link);
        }

        public void SetDisqualificationReason(string disqualificationReason) => DisqualificationReason = disqualificationReason;

        public void RemoveLink(NodeRedirectLink link)
        {
            if (!_links.Remove(link))
                throw new InvalidOperationException("Link not found on this redirect.");
        }
    }
}
