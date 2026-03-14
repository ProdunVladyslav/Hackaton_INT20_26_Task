using Application;
using Application.Repositories.Interfaces;
using Domain.Model.User;
using Infrastructure.Contracts.Quiz.Requests;
using Infrastructure.Contracts.Quiz.Responses;
using Infrastructure.Contracts.Flows.Responses;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.UseCases.Quiz;

/// <summary>
/// Use case: Start a new quiz session for a given published flow.
///
/// Validation:
/// - Flow must exist and be published
/// - Flow must have an entry node
///
/// Output: SessionStateResponse with the entry node and its options/offers
/// </summary>
public sealed class StartSessionUseCase
{
    private readonly IFlowRepository _flows;
    private readonly IUserSessionRepository _sessions;
    private readonly IUnitOfWork _uow;
    private readonly AppDbContext _db;

    public StartSessionUseCase(
        IFlowRepository flows,
        IUserSessionRepository sessions,
        IUnitOfWork uow,
        AppDbContext db)
    {
        _flows = flows;
        _sessions = sessions;
        _uow = uow;
        _db = db;
    }

    public async Task<FlowResult<SessionStateResponse>> ExecuteAsync(
        StartSessionRequest request,
        CancellationToken ct = default)
    {
        // Check flow exists
        var flow = await _flows.GetByIdAsync(request.FlowId, ct);
        if (flow is null)
            return FlowResult<SessionStateResponse>.NotFound("Flow not found.");

        // Check flow is published
        if (!flow.IsPublished)
            return FlowResult<SessionStateResponse>.Fail("Flow is not published.", 422);

        // Check entry node exists
        if (flow.EntryNodeId is null)
            return FlowResult<SessionStateResponse>.Fail("Flow has no entry node.", 422);

        // Create session
        var session = UserSession.Create(request.FlowId, flow.EntryNodeId.Value);
        session.SetUtm(request.UtmSource, request.UtmCampaign);

        // Persist
        await _sessions.AddAsync(session, ct);
        await _uow.SaveChangesAsync();

        // Load entry node with options and offers
        var currentNode = await BuildCurrentNodeAsync(flow.EntryNodeId.Value, ct);
        if (currentNode is null)
            return FlowResult<SessionStateResponse>.NotFound("Entry node not found.");

        var response = new SessionStateResponse(
            SessionId: session.Id,
            FlowId: session.FlowId,
            Status: session.Status.ToString(),
            StartedAt: session.StartedAt,
            CompletedAt: session.CompletedAt,
            CurrentNode: currentNode
        );

        return FlowResult<SessionStateResponse>.Ok(response);
    }

    private async Task<CurrentNodeResponse?> BuildCurrentNodeAsync(Guid nodeId, CancellationToken ct)
    {
        var node = await _db.Nodes.Include(n => n.Options).FirstOrDefaultAsync(n => n.Id == nodeId, ct);
        if (node is null)
            return null;

        var nodeOfferData = await _db.NodeOffers
            .Where(no => no.NodeId == nodeId)
            .Join(_db.Offers, no => no.OfferId, o => o.Id, (no, o) => new { no, o })
            .ToListAsync(ct);

        return new CurrentNodeResponse(
            Id: node.Id,
            Type: node.Type.ToString(),
            AttributeKey: node.AttributeKey,
            Title: node.Title,
            Description: node.Description,
            MediaUrl: node.MediaUrl,
            Options: node.Options.OrderBy(o => o.DisplayOrder)
                .Select(o => new QuizOptionResponse(o.Id, o.Label, o.Value, o.DisplayOrder, o.MediaUrl))
                .ToList(),
            Offers: nodeOfferData
                .Select(x => new QuizOfferResponse(x.o.Id, x.o.Name, x.o.Slug, x.o.Price, x.o.ImageUrl, x.o.CtaText, x.o.CtaUrl, x.no.IsPrimary))
                .ToList()
        );
    }
}
