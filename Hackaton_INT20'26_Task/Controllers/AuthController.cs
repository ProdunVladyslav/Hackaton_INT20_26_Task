using System.Security.Claims;
using Infrastructure.Contracts.Auth.Requests;
using Infrastructure.Contracts.Auth.Responses;
using Infrastructure.UseCases.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Hackaton_INT20_26_Task.Controllers;

/// <summary>
/// Handles authentication: login, current-user info, and logout.
///
/// Design philosophy:
///   Controllers are intentionally thin — each action delegates all business
///   logic to a dedicated use case and handles only HTTP concerns:
///   - Mapping the result to the correct HTTP status code.
///   - Writing / deleting the HttpOnly cookie.
///   - Returning the correct response DTO.
///
/// Cookie strategy:
///   The JWT is stored in an HttpOnly cookie named "access_token".
///   HttpOnly  → JavaScript cannot read it          (XSS immune).
///   SameSite  → Not sent on cross-site requests    (CSRF immune).
///   Secure    → Only sent over HTTPS in production.
///   The browser attaches the cookie automatically on every same-origin request,
///   so the frontend has zero token-management code to write.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Auth")]
public sealed class AuthController : ControllerBase
{
    private readonly LoginUseCase _loginUseCase;
    private readonly MeUseCase    _meUseCase;

    public AuthController(LoginUseCase loginUseCase, MeUseCase meUseCase)
    {
        _loginUseCase = loginUseCase;
        _meUseCase    = meUseCase;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/auth/login
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Authenticates a user and sets an HttpOnly JWT cookie.</summary>
    /// <remarks>
    /// Send email + password in the JSON body. On success the server writes an
    /// <c>access_token</c> HttpOnly cookie — **you do not need to handle the token**,
    /// the browser will attach it automatically to every subsequent request.
    ///
    /// **Swagger note:** after calling this endpoint once, the Swagger UI will include
    /// the cookie on every "Try it out" request automatically.
    /// </remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary     = "Login",
        Description = "Validates credentials. On success, writes an HttpOnly 'access_token' " +
                      "cookie — no token handling required on the client side.",
        OperationId = "Auth_Login")]
    [SwaggerResponse(200, "Login successful. 'access_token' cookie has been set.", typeof(LoginResponse))]
    [SwaggerResponse(400, "Validation error — missing or malformed fields.")]
    [SwaggerResponse(401, "Invalid email or password.")]
    [SwaggerResponse(423, "Account is temporarily locked out due to too many failed attempts.")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _loginUseCase.ExecuteAsync(request);

        if (!result.Success)
        {
            // Distinguish lockout (423 Locked) from wrong credentials (401).
            if (result.ErrorMessage!.Contains("locked", StringComparison.OrdinalIgnoreCase))
                return StatusCode(StatusCodes.Status423Locked, new { message = result.ErrorMessage });

            return Unauthorized(new { message = result.ErrorMessage });
        }

        // ── Set the HttpOnly cookie ───────────────────────────────────────────
        // The use case returned the raw JWT string. We embed it in an HttpOnly
        // cookie so JavaScript can never read it. The browser will send this
        // cookie on every subsequent same-origin request automatically.
        Response.Cookies.Append("access_token", result.Token!, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,

            // Secure = true in production (HTTPS only). In local dev over plain
            // http://localhost we relax this so the cookie still works.
            Secure   = !HttpContext.Request.Host.Host.Equals(
                           "localhost", StringComparison.OrdinalIgnoreCase),

            // Mirror the token expiry in the cookie so they expire together.
            // The browser will stop sending the cookie once it expires.
            Expires  = DateTimeOffset.UtcNow.AddHours(1),

            Path     = "/"  // Cookie is valid for the whole API, not just /api/auth.
        });

        // Return the public user info in the body — token never leaks here.
        return Ok(result.User);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/auth/me
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Returns profile information for the currently authenticated user.</summary>
    /// <remarks>
    /// Requires the <c>access_token</c> cookie to be present and valid.
    /// The user's identity is read from the JWT <c>sub</c> claim — no extra parameter needed.
    ///
    /// **Swagger note:** call <c>POST /api/auth/login</c> first; the cookie will be
    /// attached automatically to this request in the Swagger UI.
    /// </remarks>
    [HttpGet("me")]
    [Authorize]
    [SwaggerOperation(
        Summary     = "Get current user",
        Description = "Returns profile information for the authenticated user. " +
                      "Requires a valid 'access_token' cookie (set by POST /api/auth/login).",
        OperationId = "Auth_Me")]
    [SwaggerResponse(200, "Current user's information.", typeof(MeResponse))]
    [SwaggerResponse(401, "Not authenticated — 'access_token' cookie is missing or expired.")]
    [SwaggerResponse(404, "User record not found (token is valid but user was deleted).")]
    public async Task<IActionResult> Me()
    {
        // Extract the user ID from the JWT 'sub' claim.
        // ASP.NET Core's JWT Bearer middleware maps the 'sub' claim to
        // ClaimTypes.NameIdentifier by default.
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue(JwtClaimNames.Sub);

        if (userIdStr is null || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(new { message = "Unable to identify user from token." });

        var response = await _meUseCase.ExecuteAsync(userId);

        return response is null
            ? NotFound(new { message = "User not found." })
            : Ok(response);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/auth/logout
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Clears the authentication cookie, ending the session.</summary>
    /// <remarks>
    /// Deletes the <c>access_token</c> cookie from the browser.
    /// Because JWT is stateless there is no server-side session to destroy;
    /// the token simply stops being sent after this call.
    ///
    /// **Important:** if you need immediate token revocation (e.g., after a password
    /// change), implement a server-side deny-list using the JWT's <c>jti</c> claim.
    /// </remarks>
    [HttpPost("logout")]
    [Authorize]
    [SwaggerOperation(
        Summary     = "Logout",
        Description = "Removes the 'access_token' cookie from the browser. " +
                      "The JWT is stateless — no server-side invalidation occurs.",
        OperationId = "Auth_Logout")]
    [SwaggerResponse(200, "Logged out successfully. Cookie has been cleared.")]
    [SwaggerResponse(401, "Not authenticated.")]
    public IActionResult Logout()
    {
        // Delete the cookie by overwriting it with an expired one.
        // Must match the same Path and HttpOnly/Secure flags used when setting it.
        Response.Cookies.Delete("access_token", new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure   = !HttpContext.Request.Host.Host.Equals(
                           "localhost", StringComparison.OrdinalIgnoreCase),
            Path     = "/"
        });

        return Ok(new { message = "Logged out successfully." });
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    // Alias for the 'sub' claim name string, avoiding a magic string in the action.
    private static class JwtClaimNames
    {
        public const string Sub = "sub";
    }
}
