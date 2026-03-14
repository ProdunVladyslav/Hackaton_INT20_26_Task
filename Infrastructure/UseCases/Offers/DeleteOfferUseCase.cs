using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Flows.Responses;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.UseCases.Offers;

public sealed class DeleteOfferUseCase
{
    private readonly IOfferRepository _offerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteOfferUseCase(IOfferRepository offerRepository, IUnitOfWork unitOfWork)
    {
        _offerRepository = offerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<FlowResult<bool>> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var offer = await _offerRepository.GetByIdAsync(id, ct);
        if (offer is null)
            return FlowResult<bool>.NotFound("Offer not found.");

        try
        {
            _offerRepository.Remove(offer);
            await _unitOfWork.SaveChangesAsync(ct);

            return FlowResult<bool>.Ok(true);
        }
        catch (DbUpdateException ex)
        {
            // DB Restrict constraint violation: offer is linked to nodes
            return FlowResult<bool>.Fail(
                "Offer is linked to nodes. Unlink it first.",
                409);
        }
    }
}
