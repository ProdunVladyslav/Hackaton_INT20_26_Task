using System.Security.Claims;
using Infrastructure.Contracts.Auth.Requests;
using Infrastructure.Contracts.Auth.Responses;
using Infrastructure.UseCases.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Domain.Model.Auth;
using Swashbuckle.AspNetCore.Annotations;

namespace Hackaton_INT20_26_Task.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Auth")]
public sealed class AuthController : ControllerBase
{
    private readonly LoginUseCase _loginUseCase;
    private readonly MeUseCase _meUseCase;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AuthController(
        LoginUseCase loginUseCase,
        MeUseCase meUseCase,
        SignInManager<ApplicationUser> signInManager)
    {
        _loginUseCase = loginUseCase;
        _meUseCase = meUseCase;
        _signInManager = signInManager;
    }

    // POST /api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Login",
        Description = "Validates credentials. On success, sets an HttpOnly Identity cookie automatically.",
        OperationId = "Auth_Login")]
    [SwaggerResponse(200, "Login successful.", typeof(LoginResponse))]
    [SwaggerResponse(400, "Validation error.")]
    [SwaggerResponse(401, "Invalid email or password.")]
    [SwaggerResponse(423, "Account is temporarily locked out.")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _loginUseCase.ExecuteAsync(request);

        if (!result.Success)
        {
            if (result.ErrorMessage!.Contains("locked", StringComparison.OrdinalIgnoreCase))
                return StatusCode(StatusCodes.Status423Locked, new { message = result.ErrorMessage });

            return Unauthorized(new { message = result.ErrorMessage });
        }

        // Cookie is already set by SignInManager.PasswordSignInAsync — just return the user.
        return Ok(result.User);
    }

    // GET /api/auth/me
    [HttpGet("me")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Get current user",
        Description = "Returns profile information for the authenticated user.",
        OperationId = "Auth_Me")]
    [SwaggerResponse(200, "Current user's information.", typeof(MeResponse))]
    [SwaggerResponse(401, "Not authenticated.")]
    [SwaggerResponse(404, "User record not found.")]
    public async Task<IActionResult> Me()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userIdStr is null || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(new { message = "Unable to identify user." });

        var response = await _meUseCase.ExecuteAsync(userId);

        return response is null
            ? NotFound(new { message = "User not found." })
            : Ok(response);
    }

    // POST /api/auth/logout
    [HttpPost("logout")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Logout",
        Description = "Signs out and clears the Identity authentication cookie.",
        OperationId = "Auth_Logout")]
    [SwaggerResponse(200, "Logged out successfully.")]
    [SwaggerResponse(401, "Not authenticated.")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { message = "Logged out successfully." });
    }
}
