using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.Offers.Responses;

namespace Infrastructure.UseCases.Offers;

public sealed class GetOfferUseCase(
    IOfferRepository _offerRepository,
    IUserProfileRepository _userProfiles)
{
    public async Task<FlowResult<OfferResponse>> ExecuteAsync(
        Guid id,
        Guid applicationUserId,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(
            p => p.ApplicationUserId == applicationUserId, ct);
        if (profile is null)
            return FlowResult<OfferResponse>.NotFound("User profile not found.");

        var offer = await _offerRepository.GetByIdAsync(id, ct);
        if (offer is null || offer.OwnerId != profile.Id)
            return FlowResult<OfferResponse>.NotFound("Offer not found.");

        return FlowResult<OfferResponse>.Ok(ToResponse(offer));
    }

    private static OfferResponse ToResponse(Offer o) =>
        new(o.Id, o.Slug, o.Name, o.Headline, o.Body,
            o.ImageUrl, o.CalendarUrl, o.CtaText, o.CtaUrl);
}