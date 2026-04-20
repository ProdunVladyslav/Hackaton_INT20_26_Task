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
public sealed class ConvertUseCase(
    IUserSessionRepository _sessions,
    IOfferRepository _offers,
    ISessionOfferRepository _sessionOffers,
    IUnitOfWork _uow)
{
    public async Task<FlowResult<bool>> ExecuteAsync(
    Guid sessionId, ConvertRequest request, CancellationToken ct = default)
    {
        var session = await _sessions.GetByIdAsync(sessionId, ct);
        if (session is null)
            return FlowResult<bool>.NotFound("Session not found.");

        // Session must be completed — conversion only valid at terminal node
        if (session.Status != SessionStatus.Completed)
            return FlowResult<bool>.Fail("Cannot convert an in-progress session.", 422);

        var offer = await _offers.GetByIdAsync(request.OfferId, ct);
        if (offer is null)
            return FlowResult<bool>.NotFound("Offer not found.");

        var sessionOffer = await _sessionOffers.GetBySessionAndOfferAsync(
            sessionId, request.OfferId, ct);

        if (sessionOffer is null)
        {
            // Offer was not tracked via TrackOfferImpressionsAsync —
            // create the record and mark converted in one step
            sessionOffer = SessionOffer.Create(sessionId, request.OfferId, false);
            sessionOffer.MarkConverted();
            await _sessionOffers.AddAsync(sessionOffer, ct);
        }
        else
        {
            sessionOffer.MarkConverted();
            _sessionOffers.Update(sessionOffer);
        }

        await _uow.SaveChangesAsync(ct);
        return FlowResult<bool>.Ok(true);
    }
}
