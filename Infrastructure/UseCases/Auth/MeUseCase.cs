using Application.Contracts.Auth;
using Application.Repositories.Interfaces;

namespace Infrastructure.UseCases.Auth;

/// <summary>
/// Use case: return the full profile of the currently authenticated user.
///
/// WHY AppDbContext instead of UserManager.FindByIdAsync:
///   UserManager loads only the ApplicationUser row. To also load the related
///   UserProfile in one round-trip we need an EF query with .Include(). Injecting
///   AppDbContext directly is the pragmatic hackathon choice; swap for a proper
///   repository interface when the project grows.
///
/// Input:
///   userId – extracted from the JWT 'sub' claim by the controller. Passing it
///   as a plain Guid keeps the use case HTTP-agnostic (no IHttpContextAccessor).
///
/// Output:
///   MeResponse if found, null if the token references a deleted/missing user
///   (the controller turns that into a 404).
/// </summary>
public sealed class MeUseCase(IUserProfileRepository userProfileRepository)
{
    public async Task<MeResponse?> ExecuteAsync(Guid userId, CancellationToken ct = default)
        => await userProfileRepository.GetMeAsync(userId, ct);
}
