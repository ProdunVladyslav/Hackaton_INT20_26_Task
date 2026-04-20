namespace Infrastructure.Contracts.NodeRedirectLinks.Requests
{
    public sealed record CreateNodeRedirectLinkRequest(
        string Label,
        string Url,
        int DisplayOrder);
}
