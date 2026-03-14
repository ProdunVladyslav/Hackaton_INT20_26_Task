namespace Infrastructure.Contracts.Auth.Responses;

/// <summary>
/// Payload returned by GET /api/auth/me — the currently authenticated user.
///
/// ProfileId being nullable:
///   A UserProfile is not created automatically at registration.
///   The profile row is created lazily (e.g., on first edit) or as part of
///   a registration flow you add later. Null here simply means the user
///   authenticated but hasn't had a profile record created yet.
///   Add more profile fields here as the hackathon task makes them clear.
/// </summary>
public sealed record MeResponse(
    /// <summary>The user's unique identifier (matches the JWT 'sub' claim).</summary>
    Guid UserId,

    /// <summary>The user's e-mail address.</summary>
    string Email,

    /// <summary>The user's login / display name.</summary>
    string UserName,

    /// <summary>The linked UserProfile row ID, or null if no profile exists yet.</summary>
    Guid? ProfileId
);
