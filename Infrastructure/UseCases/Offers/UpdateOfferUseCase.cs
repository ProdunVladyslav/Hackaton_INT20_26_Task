using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.Offers.Requests;
using Infrastructure.Contracts.Offers.Responses;

namespace Infrastructure.UseCases.Offers;

public sealed class UpdateOfferUseCase(
    IOfferRepository _offerRepository,
    IUserProfileRepository _userProfiles,
    IUnitOfWork _unitOfWork)
{
    public async Task<FlowResult<OfferResponse>> ExecuteAsync(
        Guid id,
        Guid applicationUserId,
        UpdateOfferRequest request,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(
            p => p.ApplicationUserId == applicationUserId, ct);
        if (profile is null)
            return FlowResult<OfferResponse>.NotFound("User profile not found.");

        var offer = await _offerRepository.GetByIdAsync(id, ct);
        if (offer is null || offer.OwnerId != profile.Id)
            return FlowResult<OfferResponse>.NotFound("Offer not found.");

        try
        {
            if (request.Slug is not null && request.Slug != offer.Slug)
            {
                var slugExists = await _offerRepository.SlugExistsAsync(request.Slug, id, ct);
                if (slugExists)
                    return FlowResult<OfferResponse>.Fail(
                        "Offer with this slug already exists.", 409);

                offer.SetSlug(request.Slug);
            }

            if (request.Name is not null) offer.SetName(request.Name);
            if (request.Headline is not null) offer.SetHeadline(request.Headline);
            if (request.Body is not null) offer.SetBody(request.Body);
            if (request.ImageUrl is not null) offer.SetImageUrl(request.ImageUrl);
            if (request.CalendarUrl is not null) offer.SetCalendarUrl(request.CalendarUrl);

            // Both CTA fields updated together to preserve entity invariant
            if (request.CtaText is not null || request.CtaUrl is not null)
                offer.SetCta(
                    request.CtaText ?? offer.CtaText,
                    request.CtaUrl ?? offer.CtaUrl);

            _offerRepository.Update(offer);
            await _unitOfWork.SaveChangesAsync(ct);

            return FlowResult<OfferResponse>.Ok(ToResponse(offer));
        }
        catch (ArgumentException ex)
        {
            return FlowResult<OfferResponse>.Fail(ex.Message, 400);
        }
    }

    private static OfferResponse ToResponse(Offer o) =>
        new(o.Id, o.Slug, o.Name, o.Headline, o.Body,
            o.ImageUrl, o.CalendarUrl, o.CtaText, o.CtaUrl);
}