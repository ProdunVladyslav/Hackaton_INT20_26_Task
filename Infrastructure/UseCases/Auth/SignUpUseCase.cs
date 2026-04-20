using Application.Repositories.Interfaces;
using Domain.Model.AdminProfile;
using Domain.Model.Auth;
using Infrastructure.Contracts.Auth.Requests;
using Infrastructure.Contracts.Auth.Responses;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.UseCases.Auth
{
    /// <summary>
    /// Use case: register a new user with email + password.
    ///
    /// Flow:
    ///   1. UserManager.CreateAsync — creates ApplicationUser, runs password validation.
    ///   2. Creates a UserProfile linked to the new user.
    ///   3. SignInManager.SignInAsync — writes the Identity cookie immediately.
    ///
    /// Password rules come from Identity config in Program.cs:
    ///   - MinLength: 8
    ///   - RequireDigit: true
    ///   - RequireUppercase: false
    ///   - RequireNonAlphanumeric: false
    ///
    /// On validation failure, UserManager returns an IdentityResult with a list
    /// of IdentityError objects — we surface the first one to the caller.
    /// </summary>
    public sealed class SignUpUseCase(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IUserProfileRepository userProfileRepository,
        IUnitOfWork unitOfWork)
    {
        public async Task<SignUpResult> ExecuteAsync(SignUpRequest request)
        {
            // Duplicate email check is handled by UserManager (RequireUniqueEmail = true)
            // so no manual check needed here.
            var user = new ApplicationUser
            {
                Email = request.Email,
                UserName = request.Email   // Identity requires UserName — use email as username
            };

            var createResult = await userManager.CreateAsync(user, request.Password);

            if (!createResult.Succeeded)
            {
                // IdentityError.Description is human-readable:
                // "Passwords must have at least one digit."
                // "Email 'x' is already taken."
                var error = createResult.Errors.First().Description;
                return new SignUpResult(Success: false, ErrorMessage: error);
            }

            // Create the domain profile linked to the new user
            var profile = new UserProfile
            {
                ApplicationUserId = user.Id
            };

            await userProfileRepository.AddAsync(profile);
            await unitOfWork.SaveChangesAsync();

            // Sign in immediately — writes the auth cookie, same as after login
            await signInManager.SignInAsync(user, isPersistent: true);

            return new SignUpResult(
                Success: true,
                User: new LoginResponse(user.Id, user.Email!, user.UserName!)
            );
        }
    }
}
