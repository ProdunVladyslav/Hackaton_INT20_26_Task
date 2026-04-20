using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.Offers.Responses;

namespace Infrastructure.UseCases.Offers;

public sealed class ListOffersUseCase(
    IOfferRepository _offerRepository,
    IUserProfileRepository _userProfiles)
{
    public async Task<FlowResult<List<OfferResponse>>> ExecuteAsync(
        Guid applicationUserId,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(
            p => p.ApplicationUserId == applicationUserId, ct);
        if (profile is null)
            return FlowResult<List<OfferResponse>>.NotFound("User profile not found.");

        var offers = await _offerRepository.GetAllOrderedByOwnerAsync(profile.Id, ct);

        return FlowResult<List<OfferResponse>>.Ok(
            offers.Select(ToResponse).ToList());
    }

    private static OfferResponse ToResponse(Offer o) =>
        new(o.Id, o.Slug, o.Name, o.Headline, o.Body,
            o.ImageUrl, o.CalendarUrl, o.CtaText, o.CtaUrl);
}