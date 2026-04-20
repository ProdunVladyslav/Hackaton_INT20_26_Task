using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.Offers.Requests;
using Infrastructure.Contracts.Offers.Responses;

namespace Infrastructure.UseCases.Offers;

public sealed class CreateOfferUseCase(
    IOfferRepository _offerRepository,
    IUserProfileRepository _userProfiles,
    IUnitOfWork _unitOfWork)
{
    public async Task<FlowResult<OfferResponse>> ExecuteAsync(
        Guid applicationUserId,
        CreateOfferRequest request,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(
            p => p.ApplicationUserId == applicationUserId, ct);
        if (profile is null)
            return FlowResult<OfferResponse>.NotFound("User profile not found.");

        var slugExists = await _offerRepository.SlugExistsAsync(request.Slug, null, ct);
        if (slugExists)
            return FlowResult<OfferResponse>.Fail("Offer with this slug already exists.", 409);

        try
        {
            var offer = Offer.Create(request.Slug, request.Name, profile.Id);

            if (request.Headline is not null) offer.SetHeadline(request.Headline);
            if (request.Body is not null) offer.SetBody(request.Body);
            if (request.ImageUrl is not null) offer.SetImageUrl(request.ImageUrl);
            if (request.CalendarUrl is not null) offer.SetCalendarUrl(request.CalendarUrl);

            offer.SetCta(request.CtaText ?? string.Empty, request.CtaUrl ?? string.Empty);

            await _offerRepository.AddAsync(offer, ct);
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