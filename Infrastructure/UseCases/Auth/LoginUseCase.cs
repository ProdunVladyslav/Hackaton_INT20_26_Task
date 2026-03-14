using Domain.Model.Auth;
using Infrastructure.Contracts.Auth.Requests;
using Infrastructure.Contracts.Auth.Responses;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.UseCases.Auth;

/// <summary>
/// Use case: authenticate a user with email + password.
///
/// Uses SignInManager.PasswordSignInAsync which:
///   1. Validates the password.
///   2. Tracks lockout attempts.
///   3. Writes the Identity cookie automatically.
/// </summary>
public sealed class LoginUseCase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public LoginUseCase(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<LoginResult> ExecuteAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Fail("Invalid email or password.");

        var signInResult = await _signInManager.PasswordSignInAsync(
            user, request.Password, isPersistent: true, lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
            return Fail("Account is temporarily locked. Try again in 5 minutes.");

        if (signInResult.IsNotAllowed)
            return Fail("Sign-in is not allowed. Please confirm your e-mail first.");

        if (!signInResult.Succeeded)
            return Fail("Invalid email or password.");

        // Cookie was set by PasswordSignInAsync — no token to return.
        return new LoginResult(
            Success: true,
            Token: null,
            User: new LoginResponse(user.Id, user.Email!, user.UserName!)
        );
    }

    private static LoginResult Fail(string message) =>
        new(Success: false, ErrorMessage: message);
}
