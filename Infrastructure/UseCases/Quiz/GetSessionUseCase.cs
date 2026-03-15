using Application;
using Application.Repositories.Interfaces;
using Domain.Model.User;
using Infrastructure.Contracts.Quiz.Responses;
using Infrastructure.Contracts.Flows.Responses;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.UseCases.Quiz;

/// <summary>
/// Use case: Get the current state of a quiz session.
///
/// If session is completed, CurrentNode is null.
/// Otherwise, returns the current node with options and offers.
/// </summary>
public sealed class GetSessionUseCase
{
    private readonly IUserSessionRepository _sessions;
    private readonly AppDbContext _db;

    public GetSessionUseCase(IUserSessionRepository sessions, AppDbContext db)
    {
        _sessions = sessions;
        _db = db;
    }

    public async Task<FlowResult<SessionStateResponse>> ExecuteAsync(Guid sessionId, CancellationToken ct = default)
    {
        // Load session
        var session = await _sessions.GetByIdAsync(sessionId, ct);
        if (session is null)
            return FlowResult<SessionStateResponse>.NotFound("Session not found.");

        // Load current node (even for completed sessions — offers must be visible)
        var currentNode = await BuildCurrentNodeAsync(session.CurrentNodeId, ct);

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
                .Select(x => new QuizOfferResponse(
                    x.o.Id, x.o.Name, x.o.Slug,
                    x.o.Description, x.o.Duration, x.o.DigitalContent,
                    x.o.PhysicalWellnessKitName, x.o.PhysicalWellnessKitItems,
                    x.o.Price, x.o.ImageUrl, x.o.CtaText, x.o.CtaUrl,
                    x.no.IsPrimary))
                .ToList()
        );
    }
}
