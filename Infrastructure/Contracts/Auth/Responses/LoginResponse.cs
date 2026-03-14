namespace Infrastructure.Contracts.Auth.Responses;

/// <summary>
/// The payload the HTTP client receives in the response body after a successful login.
///
/// WHY no token here:
///   The JWT lives exclusively in the HttpOnly cookie set by the controller.
///   Putting it in the JSON body would allow JavaScript to read it, defeating
///   the purpose of the HttpOnly cookie (XSS protection).
///   The frontend only needs to know WHO is logged in — the browser handles
///   sending the credential on every subsequent request transparently.
/// </summary>
public sealed record LoginResponse(
    /// <summary>The user's unique identifier.</summary>
    Guid UserId,

    /// <summary>The authenticated user's e-mail address.</summary>
    string Email,

    /// <summary>The authenticated user's display / login name.</summary>
    string UserName
);
