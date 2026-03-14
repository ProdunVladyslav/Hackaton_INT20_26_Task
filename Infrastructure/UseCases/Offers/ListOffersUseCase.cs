using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Flows.Responses;
using Infrastructure.Contracts.Offers.Responses;

namespace Infrastructure.UseCases.Offers;

public sealed class ListOffersUseCase
{
    private readonly IOfferRepository _offerRepository;

    public ListOffersUseCase(IOfferRepository offerRepository)
    {
        _offerRepository = offerRepository;
    }

    public async Task<FlowResult<List<OfferResponse>>> ExecuteAsync(CancellationToken ct = default)
    {
        var offers = await _offerRepository.GetAllOrderedAsync(ct);

        var responses = offers.Select(o => new OfferResponse(
            Id: o.Id,
            Slug: o.Slug,
            Name: o.Name,
            Description: o.Description,
            Duration: o.Duration,
            DigitalContent: o.DigitalContent,
            KitName: o.KitName,
            KitContents: o.KitContents,
            Price: o.Price,
            ImageUrl: o.ImageUrl,
            CtaText: o.CtaText,
            CtaUrl: o.CtaUrl
        )).ToList();

        return FlowResult<List<OfferResponse>>.Ok(responses);
    }
}
