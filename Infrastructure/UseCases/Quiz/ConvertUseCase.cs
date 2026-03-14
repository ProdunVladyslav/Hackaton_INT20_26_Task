using Application;
using Application.Repositories.Interfaces;
using Domain.Model.User;
using Infrastructure.Contracts.Quiz.Requests;
using Infrastructure.Contracts.Flows.Responses;

namespace Infrastructure.UseCases.Quiz;

/// <summary>
/// Use case: Mark an offer as converted (user clicked CTA / purchased).
///
/// Logic:
/// 1. Load session (can be InProgress or Completed)
/// 2. Verify offer exists
/// 3. Find or create SessionOffer record
/// 4. Mark as converted
/// </summary>
public sealed class ConvertUseCase
{
    private readonly IUserSessionRepository _sessions;
    private readonly IOfferRepository _offers;
    private readonly ISessionOfferRepository _sessionOffers;
    private readonly IUnitOfWork _uow;

    public ConvertUseCase(
        IUserSessionRepository sessions,
        IOfferRepository offers,
        ISessionOfferRepository sessionOffers,
        IUnitOfWork uow)
    {
        _sessions = sessions;
        _offers = offers;
        _sessionOffers = sessionOffers;
        _uow = uow;
    }

    public async Task<FlowResult<bool>> ExecuteAsync(Guid sessionId, ConvertRequest request, CancellationToken ct = default)
    {
        // Load session (can be InProgress or Completed)
        var session = await _sessions.GetByIdAsync(sessionId, ct);
        if (session is null)
            return FlowResult<bool>.NotFound("Session not found.");

        // Verify offer exists
        var offer = await _offers.GetByIdAsync(request.OfferId, ct);
        if (offer is null)
            return FlowResult<bool>.NotFound("Offer not found.");

        // Find or create SessionOffer
        var sessionOffer = await _sessionOffers.GetBySessionAndOfferAsync(sessionId, request.OfferId, ct);
        if (sessionOffer is null)
        {
            sessionOffer = SessionOffer.Create(sessionId, request.OfferId, false);
            await _sessionOffers.AddAsync(sessionOffer, ct);
        }
        else
        {
            sessionOffer.MarkConverted();
            _sessionOffers.Update(sessionOffer);
        }

        await _uow.SaveChangesAsync();
        return FlowResult<bool>.Ok(true);
    }
}
