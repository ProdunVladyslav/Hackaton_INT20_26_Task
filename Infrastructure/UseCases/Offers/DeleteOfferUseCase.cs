using Application.Repositories.Interfaces;
using Infrastructure.Contracts.Flows.Responses;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.UseCases.Offers;

public sealed class DeleteOfferUseCase(
    IOfferRepository _offerRepository,
    IUserProfileRepository _userProfiles,
    IUnitOfWork _unitOfWork)
{
    public async Task<FlowResult<bool>> ExecuteAsync(
        Guid id,
        Guid applicationUserId,
        CancellationToken ct = default)
    {
        var profile = await _userProfiles.FirstOrDefaultAsync(p => p.ApplicationUserId == applicationUserId, ct);
        if (profile == null)
            return FlowResult<bool>.NotFound("User profile not found.");

        var offer = await _offerRepository.GetByIdAsync(id, ct);
        if (offer is null)
            return FlowResult<bool>.NotFound("Offer not found.");

        if (offer.OwnerId != profile.Id)
            return FlowResult<bool>.NotFound("Offer not found.");

        try
        {
            _offerRepository.Remove(offer);
            await _unitOfWork.SaveChangesAsync(ct);

            return FlowResult<bool>.Ok(true);
        }
        catch (DbUpdateException)
        {
            return FlowResult<bool>.Fail("Offer is linked to nodes. Unlink it first.", 409);
        }
    }
}
