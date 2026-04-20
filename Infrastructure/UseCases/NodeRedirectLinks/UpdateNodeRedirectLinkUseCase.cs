using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.NodeRedirectLinks.Requests;
using Infrastructure.Contracts.NodeRedirectLinks.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.UseCases.NodeRedirectLinks
{
    /// <summary>
    /// Patch-style update for a single link — only provided fields are applied.
    /// </summary>
    public sealed class UpdateNodeRedirectLinkUseCase(
        INodeRepository _nodes,
        INodeRedirectLinkRepository _links,
        IFlowRepository _flows,
        IUserProfileRepository _userProfiles,
        IUnitOfWork _uow)
    {
        public async Task<FlowResult<NodeRedirectLinkResponse>> ExecuteAsync(
            Guid nodeId,
            Guid linkId,
            Guid applicationUserId,
            UpdateNodeRedirectLinkRequest request,
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

            var link = await _links.GetByIdAsync(linkId, ct);
            if (link == null)
                return FlowResult<NodeRedirectLinkResponse>.NotFound("Link not found.");

            if (link.NodeRedirectId != node.Redirect.Id)
                return FlowResult<NodeRedirectLinkResponse>.NotFound("Link not found on this node.");

            try
            {
                if (!string.IsNullOrWhiteSpace(request.Label))
                    link.SetLabel(request.Label);

                if (!string.IsNullOrWhiteSpace(request.Url))
                    link.SetUrl(request.Url);

                if (request.DisplayOrder.HasValue)
                    link.SetDisplayOrder(request.DisplayOrder.Value);

                _links.Update(link);
                await _uow.SaveChangesAsync(ct);

                return FlowResult<NodeRedirectLinkResponse>.Ok(ToResponse(link));
            }
            catch (ArgumentException ex)
            {
                return FlowResult<NodeRedirectLinkResponse>.Fail(ex.Message, statusCode: 400);
            }
        }

        private static NodeRedirectLinkResponse ToResponse(NodeRedirectLink l) =>
            new(l.Id, l.NodeRedirectId, l.Label, l.Url, l.DisplayOrder);
    }
}
