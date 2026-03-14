namespace Infrastructure.Contracts.Auth.Responses;

/// <summary>
/// Internal envelope returned by LoginUseCase → AuthController.
/// Never serialised directly to the HTTP response.
///
/// WHY a separate result type:
///   The use case needs to hand the raw JWT string back to the controller
///   so the controller can embed it in an HttpOnly cookie.
///   Keeping the token out of LoginResponse (the public DTO) ensures it
///   can never accidentally leak into the JSON body.
///
/// Pattern:
///   Success = true  → Token and User are populated, ErrorMessage is null.
///   Success = false → ErrorMessage is populated, Token and User are null.
/// </summary>
public sealed record LoginResult(
    /// <summary>Whether the login attempt succeeded.</summary>
    bool Success,

    /// <summary>
    /// The signed JWT string. Present only when Success = true.
    /// The controller writes this into the 'access_token' HttpOnly cookie.
    /// </summary>
    string? Token = null,

    /// <summary>
    /// The public user payload to return in the response body.
    /// Present only when Success = true.
    /// </summary>
    LoginResponse? User = null,

    /// <summary>
    /// Human-readable failure reason. Present only when Success = false.
    /// Safe to forward to the client — does not reveal internal detail.
    /// </summary>
    string? ErrorMessage = null
);
