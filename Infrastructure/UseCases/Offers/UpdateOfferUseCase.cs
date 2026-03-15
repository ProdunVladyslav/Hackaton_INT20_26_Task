using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.Offers.Requests;
using Infrastructure.Contracts.Offers.Responses;

namespace Infrastructure.UseCases.Offers;

public sealed class UpdateOfferUseCase
{
    private readonly IOfferRepository _offerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateOfferUseCase(IOfferRepository offerRepository, IUnitOfWork unitOfWork)
    {
        _offerRepository = offerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<FlowResult<OfferResponse>> ExecuteAsync(Guid id, UpdateOfferRequest request, CancellationToken ct = default)
    {
        var offer = await _offerRepository.GetByIdAsync(id, ct);
        if (offer is null)
            return FlowResult<OfferResponse>.NotFound("Offer not found.");

        try
        {
            // If slug is changing, check uniqueness
            if (request.Slug is not null && request.Slug != offer.Slug)
            {
                var slugExists = await _offerRepository.SlugExistsAsync(request.Slug, id, ct);
                if (slugExists)
                    return FlowResult<OfferResponse>.Fail("Offer with this slug already exists.", 409);

                offer.SetSlug(request.Slug);
            }

            // Apply updates for non-null fields
            if (request.Name is not null)
                offer.SetName(request.Name);

            if (request.Description is not null)
                offer.SetDescription(request.Description);

            if (request.Duration is not null)
                offer.SetDuration(request.Duration);

            if (request.DigitalContent is not null)
                offer.SetDigitalContent(request.DigitalContent);

            if (request.PhysicalWellnessKitName is not null)
                offer.SetPhysicalWellnessKitName(request.PhysicalWellnessKitName);

            if (request.PhysicalWellnessKitItems is not null)
                offer.SetPhysicalWellnessKitItems(request.PhysicalWellnessKitItems);

            if (request.ImageUrl is not null)
                offer.SetImageUrl(request.ImageUrl);

            if (request.Price.HasValue)
                offer.SetPrice(request.Price.Value);

            if (request.CtaText is not null && request.CtaUrl is not null)
                offer.SetCta(request.CtaText, request.CtaUrl);

            // Update and save
            _offerRepository.Update(offer);
            await _unitOfWork.SaveChangesAsync(ct);

            // Return response
            var response = new OfferResponse(
                Id: offer.Id,
                Slug: offer.Slug,
                Name: offer.Name,
                Description: offer.Description,
                Duration: offer.Duration,
                DigitalContent: offer.DigitalContent,
                PhysicalWellnessKitName: offer.PhysicalWellnessKitName,
                PhysicalWellnessKitItems: offer.PhysicalWellnessKitItems,
                Price: offer.Price,
                ImageUrl: offer.ImageUrl,
                CtaText: offer.CtaText,
                CtaUrl: offer.CtaUrl
            );

            return FlowResult<OfferResponse>.Ok(response);
        }
        catch (ArgumentException ex)
        {
            return FlowResult<OfferResponse>.Fail(ex.Message, 400);
        }
    }
}
