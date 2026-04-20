using System.Security.Claims;
using Infrastructure.Contracts.Auth.Requests;
using Infrastructure.Contracts.Auth.Responses;
using Infrastructure.UseCases.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Domain.Model.Auth;
using Swashbuckle.AspNetCore.Annotations;
using Application.Contracts.Auth;

namespace Hackaton_INT20_26_Task.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Auth")]
public sealed class AuthController(
    LoginUseCase _loginUseCase,
    MeUseCase _meUseCase,
    SignInManager<ApplicationUser> _signInManager,
    SignUpUseCase _signUpUseCase) : ControllerBase
{

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

    // POST /api/auth/signup
    [HttpPost("signup")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Sign up",
        Description = "Creates a new account with email and password. Sets the auth cookie on success.",
        OperationId = "Auth_SignUp")]
    [SwaggerResponse(200, "Account created.", typeof(LoginResponse))]
    [SwaggerResponse(400, "Validation error (weak password, duplicate email, etc).")]
    public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
    {
        var result = await _signUpUseCase.ExecuteAsync(request);

        return result.Success
            ? Ok(result.User)
            : BadRequest(new { message = result.ErrorMessage });
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
