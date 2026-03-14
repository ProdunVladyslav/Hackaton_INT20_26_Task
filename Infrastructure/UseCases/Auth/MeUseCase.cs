using Application;
using Domain.Model;
using Infrastructure.Contracts.Auth.Responses;
using Microsoft.EntityFrameworkCore;

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
public sealed class MeUseCase
{
    private readonly AppDbContext _db;

    public MeUseCase(AppDbContext db) => _db = db;

    /// <summary>
    /// Loads the user and their profile by <paramref name="userId"/>.
    /// </summary>
    /// <param name="userId">The authenticated user's Guid (from JWT sub claim).</param>
    /// <returns>Populated MeResponse or null when the user no longer exists.</returns>
    public async Task<MeResponse?> ExecuteAsync(Guid userId)
    {
        // Single DB round-trip: user row + profile row via LEFT JOIN.
        // Profile is nullable — a user may not have a profile yet (see UserProfile comments).
        var user = await _db.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
            return null;

        return new MeResponse(
            UserId    : user.Id,
            Email     : user.Email!,
            UserName  : user.UserName!,
            ProfileId : user.Profile?.Id   // null-safe: profile may not exist yet
        );
    }
}
