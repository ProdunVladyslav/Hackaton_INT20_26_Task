using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Flows;

/// <summary>
/// Use case: load a single flow with its full DAG (nodes + options + edges + offers).
/// No analytics — use <see cref="GetFlowStatsUseCase"/> for session/offer stats
/// and <see cref="GetFlowLeadsUseCase"/> for lead capture data.
///
/// Queries executed:
///   1. Flow DAG     — nodes, options, edges via repository
///   2. NodeOffers   — offer links for all nodes in one batch
/// </summary>
public sealed class GetFlowUseCase(
    IFlowRepository _flows,
    IUserProfileRepository _userProfiles,
    INodeOfferRepository _nodeOffers)
{
    public async Task<FlowResult<FlowDetailResponse>> ExecuteAsync(
        Guid flowId,
        Guid applicationUserId,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(p => p.ApplicationUserId == applicationUserId, ct);
        if (profile is null)
            return FlowResult<FlowDetailResponse>.NotFound("User profile not found.");

        var flow = await _flows.GetFlowWithDagAsync(flowId, profile.Id, ct);
        if (flow is null)
            return FlowResult<FlowDetailResponse>.NotFound($"Flow {flowId} not found.");

        var nodeIds = flow.Nodes.Select(n => n.Id).ToList();
        var nodeOffersByNode = await _nodeOffers.GetByNodeIdsWithOffersAsync(nodeIds, ct);

        // ── AttributeKeys ─────────────────────────────────────────────────────
        var attributeKeys = flow.Nodes
            .Where(n => n.Type == NodeType.Question
                     && !string.IsNullOrWhiteSpace(n.AttributeKey)
                     && n.ValueKind.HasValue)
            .GroupBy(n => n.AttributeKey, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var kind = g.First().ValueKind!.Value;
                var allowedOperators = kind switch
                {
                    ValueKind.Text => new[] { "eq", "neq", "in" },
                    ValueKind.Numeric => new[] { "eq", "neq", "in", "gt", "gte", "lt", "lte", "between" },
                    _ => new[] { "eq", "neq" }
                };
                return new AttributeKeyDto(g.Key, kind.ToString(), allowedOperators);
            })
            .OrderBy(x => x.Key)
            .ToList();

        // ── Base DTO ──────────────────────────────────────────────────────────
        var detail = FlowMapper.ToDetail(flow);

        // ── Enrich nodes ──────────────────────────────────────────────────────
        var enrichedNodes = detail.Nodes.Select(nodeDto =>
        {
            var offers = nodeOffersByNode.TryGetValue(nodeDto.Id, out var list)
                ? list.Select(no => new NodeOfferDto(
                      no.Id,
                      no.OfferId,
                      no.IsPrimary,
                      new OfferDto(
                          no.Offer.Id,
                          no.Offer.Slug,
                          no.Offer.Name,
                          no.Offer.Headline,
                          no.Offer.Body,
                          no.Offer.ImageUrl,
                          no.Offer.CalendarUrl,
                          no.CalendarProvider?.ToString(),
                          no.Tier.ToString(),
                          no.Offer.CtaText,
                          no.Offer.CtaUrl
                      )
                  )).ToList()
                : new List<NodeOfferDto>();

            var domainNode = flow.Nodes.First(n => n.Id == nodeDto.Id);

            var redirectDto = domainNode.Redirect is { } r
                ? new NodeRedirectDto(
                    Id: r.Id,
                    RedirectUrl: r.RedirectUrl,
                    AutoRedirectAfterSeconds: r.AutoRedirectAfterSeconds,
                    DisqualificationReason: r.DisqualificationReason,
                    Links: r.Links.Select(l => new NodeRedirectLinkDto(
                        Id: l.Id,
                        Label: l.Label,
                        Url: l.Url
                    )).ToList().AsReadOnly())
                : null;

            var leadCaptureDto = domainNode.LeadCapture is { } lc
                ? new NodeLeadCaptureDto(
                    Id: lc.Id,
                    IsRequired: lc.IsRequired,
                    Fields: lc.Fields
                        .OrderBy(f => f.DisplayOrder)
                        .Select(f => new NodeLeadCaptureFieldDto(
                            Id: f.Id,
                            FieldType: f.FieldType.ToString(),
                            AttributeKey: f.AttributeKey,
                            IsRequired: f.IsRequired,
                            DisplayOrder: f.DisplayOrder,
                            Placeholder: f.Placeholder
                        )).ToList().AsReadOnly())
                : null;

            return nodeDto with
            {
                NodeOffers = offers,
                Redirect = redirectDto,
                LeadCapture = leadCaptureDto
            };
        }).ToList();

        return FlowResult<FlowDetailResponse>.Ok(detail with
        {
            Nodes = enrichedNodes,
            AttributeKeys = attributeKeys,
        });
    }
}