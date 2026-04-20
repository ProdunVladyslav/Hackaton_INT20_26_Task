using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.NodeRedirectLinks.Requests;
using Infrastructure.Contracts.NodeRedirectLinks.Responses;

namespace Infrastructure.UseCases.NodeRedirectLinks
{
    /// <summary>
    /// Creates a new link on the Redirect node's NodeRedirect aggregate.
    /// The node must be of type Redirect and already have a NodeRedirect entity.
    /// Maximum 3 links per NodeRedirect is enforced by the domain.
    /// </summary>
    public sealed class CreateNodeRedirectLinkUseCase(
        INodeRepository _nodes,
        INodeRedirectLinkRepository _links,
        IFlowRepository _flows,
        IUserProfileRepository _userProfiles,
        IUnitOfWork _uow)
    {
        public async Task<FlowResult<NodeRedirectLinkResponse>> ExecuteAsync(
            Guid nodeId,
            Guid applicationUserId,
            CreateNodeRedirectLinkRequest request,
            CancellationToken ct = default)
        {
            var profile = await _userProfiles.FirstOrDefaultAsync(p => p.ApplicationUserId == applicationUserId, ct);
            if (profile == null)
                return FlowResult<NodeRedirectLinkResponse>.NotFound("User profile not found.");

            var node = await _nodes.GetByIdAsync(nodeId, ct);
            if (node == null)
                return FlowResult<NodeRedirectLinkResponse>.NotFound("Node not found.");

            var flow = await _flows.FirstOrDefaultAsync(f => f.Id == node.FlowId && f.OwnerId == profile.Id, ct);
            if (flow == null)
                return FlowResult<NodeRedirectLinkResponse>.NotFound("Node not found.");

            if (node.Type != NodeType.Redirect)
                return FlowResult<NodeRedirectLinkResponse>.Fail("Only Redirect nodes can have links.", statusCode: 422);

            if (node.Redirect == null)
                return FlowResult<NodeRedirectLinkResponse>.NotFound("Redirect configuration not found on this node.");

            try
            {
                var link = NodeRedirectLink.Create(
                    node.Redirect.Id,
                    request.Label,
                    request.Url,
                    request.DisplayOrder);

                // Domain enforces max-3 rule
                node.Redirect.AddLink(link);

                await _links.AddAsync(link, ct);
                await _uow.SaveChangesAsync(ct);

                return FlowResult<NodeRedirectLinkResponse>.Ok(ToResponse(link));
            }
            catch (ArgumentException ex)
            {
                return FlowResult<NodeRedirectLinkResponse>.Fail(ex.Message, statusCode: 400);
            }
            catch (InvalidOperationException ex)
            {
                return FlowResult<NodeRedirectLinkResponse>.Fail(ex.Message, statusCode: 422);
            }
        }

        private static NodeRedirectLinkResponse ToResponse(NodeRedirectLink l) =>
            new(l.Id, l.NodeRedirectId, l.Label, l.Url, l.DisplayOrder);
    }
}
