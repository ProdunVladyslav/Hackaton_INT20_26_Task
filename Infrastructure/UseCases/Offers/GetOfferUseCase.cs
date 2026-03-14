using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.Offers.Responses;

namespace Infrastructure.UseCases.Offers;

public sealed class GetOfferUseCase
{
    private readonly IOfferRepository _offerRepository;

    public GetOfferUseCase(IOfferRepository offerRepository)
    {
        _offerRepository = offerRepository;
    }

    public async Task<FlowResult<OfferResponse>> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var offer = await _offerRepository.GetByIdAsync(id, ct);

        if (offer is null)
            return FlowResult<OfferResponse>.NotFound("Offer not found.");

        var response = new OfferResponse(
            Id: offer.Id,
            Slug: offer.Slug,
            Name: offer.Name,
            Description: offer.Description,
            Duration: offer.Duration,
            DigitalContent: offer.DigitalContent,
            KitName: offer.KitName,
            KitContents: offer.KitContents,
            Price: offer.Price,
            ImageUrl: offer.ImageUrl,
            CtaText: offer.CtaText,
            CtaUrl: offer.CtaUrl
        );

        return FlowResult<OfferResponse>.Ok(response);
    }
}
