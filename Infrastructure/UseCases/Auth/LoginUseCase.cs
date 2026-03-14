using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Domain.Model;
using Infrastructure.Contracts.Auth.Requests;
using Infrastructure.Contracts.Auth.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.UseCases.Auth;

/// <summary>
/// Use case: authenticate a user with email + password and produce a signed JWT.
///
/// Responsibilities:
///   1. Find the user by e-mail.
///   2. Validate the password via SignInManager (also handles lockout tracking).
///   3. On success, build a JWT containing the core identity claims.
///   4. Return a LoginResult the controller uses to set the HttpOnly cookie.
///
/// WHY SignInManager.CheckPasswordSignInAsync (not PasswordSignInAsync):
///   PasswordSignInAsync writes an ASP.NET Core Identity cookie itself — we do NOT
///   want that; we are issuing a JWT cookie manually. CheckPasswordSignInAsync
///   validates the password and updates lockout counters without emitting any cookie.
///
/// WHY inject IConfiguration for JWT settings:
///   Quick and DI-friendly for a hackathon. For a larger project, replace with
///   IOptions&lt;JwtSettings&gt; and a strongly-typed options class.
/// </summary>
public sealed class LoginUseCase
{
    private readonly UserManager<ApplicationUser>   _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration                _configuration;

    public LoginUseCase(
        UserManager<ApplicationUser>   userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration                configuration)
    {
        _userManager   = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    /// <summary>
    /// Executes the login use case.
    /// </summary>
    /// <param name="request">Email + password from the HTTP request body.</param>
    /// <returns>
    /// LoginResult with Success = true  → Token holds the signed JWT; User holds the public DTO.<br/>
    /// LoginResult with Success = false → ErrorMessage explains the failure (safe to show the client).
    /// </returns>
    public async Task<LoginResult> ExecuteAsync(LoginRequest request)
    {
        // Step 1 – find the user by e-mail (case-insensitive via NormalizedEmail index).
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            // Return the same generic message for "not found" AND "wrong password"
            // to prevent user-enumeration attacks.
            return Fail("Invalid email or password.");

        // Step 2 – validate the password; also increments / resets the lockout counter.
        var signInResult = await _signInManager.CheckPasswordSignInAsync(
            user, request.Password, lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
            return Fail("Account is temporarily locked. Try again in 5 minutes.");

        if (signInResult.IsNotAllowed)
            return Fail("Sign-in is not allowed. Please confirm your e-mail first.");

        if (!signInResult.Succeeded)
            return Fail("Invalid email or password.");

        // Step 3 – mint the JWT.
        var token = BuildJwt(user);

        // Step 4 – hand the result back; the controller will set the cookie.
        return new LoginResult(
            Success : true,
            Token   : token,
            User    : new LoginResponse(user.Id, user.Email!, user.UserName!)
        );
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static LoginResult Fail(string message) =>
        new(Success: false, ErrorMessage: message);

    /// <summary>
    /// Builds and signs the JWT string.
    ///
    /// Claims embedded:
    ///   sub         → user's Guid — the canonical identity claim, used to
    ///                 re-identify the user on every request without a DB hit.
    ///   email       → handy for the frontend to display without an extra API call.
    ///   unique_name → login name.
    ///   jti         → per-token unique ID; lets you build a revocation blacklist later.
    ///   nbf / exp   → validity window (iat is added automatically by JwtSecurityToken).
    /// </summary>
    private string BuildJwt(ApplicationUser user)
    {
        var rawKey        = _configuration["Jwt:Key"]!;
        var issuer        = _configuration["Jwt:Issuer"]!;
        var audience      = _configuration["Jwt:Audience"]!;
        var expiryMinutes = int.TryParse(_configuration["Jwt:ExpiryMinutes"], out var m) ? m : 60;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,        user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email,      user.Email!),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName!),
            new Claim(JwtRegisteredClaimNames.Jti,        Guid.NewGuid().ToString()),
        };

        var signingKey  = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(rawKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer             : issuer,
            audience           : audience,
            claims             : claims,
            notBefore          : DateTime.UtcNow,
            expires            : DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials : credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
