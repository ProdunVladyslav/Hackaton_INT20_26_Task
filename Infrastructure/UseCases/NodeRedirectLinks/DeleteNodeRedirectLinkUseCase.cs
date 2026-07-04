using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.NodeRedirectLinks
{
    /// <summary>
    /// Deletes a link from the Redirect node's NodeRedirect aggregate.
    /// </summary>
    public sealed class DeleteNodeRedirectLinkUseCase(
        INodeRepository _nodes,
        INodeRedirectLinkRepository _links,
        IFlowRepository _flows,
        IUserProfileRepository _userProfiles,
        IUnitOfWork _uow)
    {
        public async Task<FlowResult<bool>> ExecuteAsync(
            Guid nodeId,
            Guid linkId,
            Guid applicationUserId,
            CancellationToken ct = default)
        {
            var profile = await _userProfiles.FirstOrDefaultAsync(p => p.ApplicationUserId == applicationUserId, ct);
            if (profile == null)
                return FlowResult<bool>.NotFound("User profile not found.");

            var node = await _nodes.GetByIdAsync(nodeId, ct);
            if (node == null)
                return FlowResult<bool>.NotFound("Node not found.");

            var flow = await _flows.FirstOrDefaultAsync(f => f.Id == node.FlowId && f.OwnerId == profile.Id, ct);
            if (flow == null)
                return FlowResult<bool>.NotFound("Node not found.");

            if (node.Type != NodeType.Redirect)
                return FlowResult<bool>.Fail("Only Redirect nodes can have links.", statusCode: 422);

            if (node.Redirect == null)
                return FlowResult<bool>.NotFound("Redirect configuration not found on this node.");

            var link = await _links.GetByIdAsync(linkId, ct);
            if (link == null)
                return FlowResult<bool>.NotFound("Link not found.");

            if (link.NodeRedirectId != node.Redirect.Id)
                return FlowResult<bool>.NotFound("Link not found on this node.");

            var isLastLink = node.Redirect.Links.Count == 1
                && string.IsNullOrWhiteSpace(node.Redirect.RedirectUrl);
            if (isLastLink)
                return FlowResult<bool>.Fail(
                    "Can't remove the last link — Redirect node needs a redirect URL or at least one link.",
                    statusCode: 422);

            _links.Remove(link);
            await _uow.SaveChangesAsync(ct);

            return FlowResult<bool>.Ok(true);
        }
    }
}
