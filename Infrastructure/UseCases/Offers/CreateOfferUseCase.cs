using Application.Repositories.Interfaces;
using Domain.Model.Survey;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.Offers.Requests;
using Infrastructure.Contracts.Offers.Responses;

namespace Infrastructure.UseCases.Offers;

public sealed class CreateOfferUseCase
{
    private readonly IOfferRepository _offerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOfferUseCase(IOfferRepository offerRepository, IUnitOfWork unitOfWork)
    {
        _offerRepository = offerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<FlowResult<OfferResponse>> ExecuteAsync(CreateOfferRequest request, CancellationToken ct = default)
    {
        // Check slug uniqueness
        var slugExists = await _offerRepository.SlugExistsAsync(request.Slug, null, ct);
        if (slugExists)
            return FlowResult<OfferResponse>.Fail("Offer with this slug already exists.", 409);

        try
        {
            // Create the offer
            var offer = Offer.Create(request.Slug, request.Name);

            // Set optional fields (setters default to "" when null, satisfying NOT NULL DB columns)
            offer.SetDescription(request.Description);
            offer.SetDuration(request.Duration);
            offer.SetDigitalContent(request.DigitalContent);
            offer.SetPhysicalWellnessKitName(request.PhysicalWellnessKitName);
            offer.SetPhysicalWellnessKitItems(request.PhysicalWellnessKitItems);
            offer.SetImageUrl(request.ImageUrl);

            if (request.Price.HasValue)
                offer.SetPrice(request.Price.Value);

            offer.SetCta(request.CtaText ?? string.Empty, request.CtaUrl ?? string.Empty);

            // Add and save
            await _offerRepository.AddAsync(offer, ct);
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
