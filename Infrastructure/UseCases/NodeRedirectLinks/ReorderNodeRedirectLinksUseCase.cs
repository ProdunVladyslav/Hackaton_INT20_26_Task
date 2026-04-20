using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.NodeRedirectLinks.Requests;
using Infrastructure.Contracts.NodeRedirectLinks.Responses;

namespace Infrastructure.UseCases.NodeRedirectLinks
{
    /// <summary>
    /// Bulk-updates DisplayOrder for links on a Redirect node.
    /// Unknown link IDs in the request are silently skipped.
    /// </summary>
    public sealed class ReorderNodeRedirectLinksUseCase(
        INodeRepository _nodes,
        INodeRedirectLinkRepository _links,
        IFlowRepository _flows,
        IUserProfileRepository _userProfiles,
        IUnitOfWork _uow)
    {
        public async Task<FlowResult<List<NodeRedirectLinkResponse>>> ExecuteAsync(
            Guid nodeId,
            Guid applicationUserId,
            ReorderNodeRedirectLinksRequest request,
            CancellationToken ct = default)
        {
            var profile = await _userProfiles.FirstOrDefaultAsync(p => p.ApplicationUserId == applicationUserId, ct);
            if (profile == null)
                return FlowResult<List<NodeRedirectLinkResponse>>.NotFound("User profile not found.");

            var node = await _nodes.GetByIdAsync(nodeId, ct);
            if (node == null)
                return FlowResult<List<NodeRedirectLinkResponse>>.NotFound("Node not found.");

            var flow = await _flows.FirstOrDefaultAsync(f => f.Id == node.FlowId && f.OwnerId == profile.Id, ct);
            if (flow == null)
                return FlowResult<List<NodeRedirectLinkResponse>>.NotFound("Node not found.");

            if (node.Type != NodeType.Redirect)
                return FlowResult<List<NodeRedirectLinkResponse>>.Fail("Only Redirect nodes can have links.", statusCode: 422);

            if (node.Redirect == null)
                return FlowResult<List<NodeRedirectLinkResponse>>.NotFound("Redirect configuration not found on this node.");

            var existingLinks = await _links.GetLinksByNodeRedirectIdAsync(node.Redirect.Id, ct);

            try
            {
                foreach (var item in request.Items)
                {
                    var link = existingLinks.FirstOrDefault(l => l.Id == item.LinkId);
                    if (link == null) continue;

                    link.SetDisplayOrder(item.DisplayOrder);
                    _links.Update(link);
                }

                await _uow.SaveChangesAsync(ct);

                var reordered = await _links.GetLinksByNodeRedirectIdAsync(node.Redirect.Id, ct);
                return FlowResult<List<NodeRedirectLinkResponse>>.Ok(reordered.Select(ToResponse).ToList());
            }
            catch (ArgumentException ex)
            {
                return FlowResult<List<NodeRedirectLinkResponse>>.Fail(ex.Message, statusCode: 400);
            }
        }

        private static NodeRedirectLinkResponse ToResponse(NodeRedirectLink l) =>
            new(l.Id, l.NodeRedirectId, l.Label, l.Url, l.DisplayOrder);
    }
}
