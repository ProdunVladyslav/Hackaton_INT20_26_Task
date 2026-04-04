using Application;
using Application.Repositories.Interfaces;
using Domain.Model.User;
using Infrastructure.Contracts.Quiz.Responses;
using Infrastructure.Contracts.Flows.Responses;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.UseCases.Quiz;

/// <summary>
/// Use case: Go back to the previous node in a quiz session.
///
/// Logic:
/// 1. Load session, verify it's InProgress
/// 2. Load all answers ordered by date
/// 3. If no answers, return error (at beginning)
/// 4. Delete the last answer
/// 5. Move session back to the previous node
/// 6. Return updated session state
/// </summary>
public sealed class GoBackUseCase
{
    private readonly IUserSessionRepository _sessions;
    private readonly IUserAnswerRepository _answers;
    private readonly IUnitOfWork _uow;
    private readonly AppDbContext _db;

    public GoBackUseCase(
        IUserSessionRepository sessions,
        IUserAnswerRepository answers,
        IUnitOfWork uow,
        AppDbContext db)
    {
        _sessions = sessions;
        _answers = answers;
        _uow = uow;
        _db = db;
    }

    public async Task<FlowResult<SessionStateResponse>> ExecuteAsync(Guid sessionId, CancellationToken ct = default)
    {
        // Load session
        var session = await _sessions.GetByIdAsync(sessionId, ct);
        if (session is null)
            return FlowResult<SessionStateResponse>.NotFound("Session not found.");

        // Verify session is in progress
        if (session.Status != SessionStatus.InProgress)
            return FlowResult<SessionStateResponse>.Fail("Session is not active.", 422);

        // Load all answers ordered by date
        var answers = await _answers.GetBySessionOrderedAsync(sessionId, ct);

        // If no answers, we're at the beginning
        if (answers.Count == 0)
            return FlowResult<SessionStateResponse>.Fail("Already at the beginning.", 422);

        // Get the last answer
        var lastAnswer = answers.Last();
        var previousNodeId = lastAnswer.NodeId;

        // Remove the last answer
        _answers.Remove(lastAnswer);

        // Move session back to previous node
        session.MoveToNode(previousNodeId);

        // Save changes
        _sessions.Update(session);
        await _uow.SaveChangesAsync();

        // Load current node
        var currentNode = await BuildCurrentNodeAsync(session.CurrentNodeId, ct);
        if (currentNode is null)
            return FlowResult<SessionStateResponse>.NotFound("Current node not found.");

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
            AnswerType: node.AnswerType.ToString(),
            ValueKind: node.ValueKind.ToString(),
            SliderMin: node.SliderMin,
            SliderMax: node.SliderMax,
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
