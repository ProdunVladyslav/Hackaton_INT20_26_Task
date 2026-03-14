using Domain.Model;
using Domain.Model.AdminProfile;
using Domain.Model.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Seeders
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var db = services.GetRequiredService<AppDbContext>();

            // ── Roles ─────────────────────────────────────────────────────────
            string[] roles = ["Admin", "User"];
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole<Guid> { Name = role });
            }

            // ── Admin users ───────────────────────────────────────────────────
            var admins = new[]
            {
            new { Email = "admin@example.com",  Password = "Admin123!" },
            new { Email = "admin2@example.com", Password = "Admin123!" },
        };

            foreach (var seed in admins)
            {
                if (await userManager.FindByEmailAsync(seed.Email) is not null)
                    continue;

                var user = new ApplicationUser
                {
                    UserName = seed.Email,
                    Email = seed.Email,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, seed.Password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to create {seed.Email}: {errors}");
                }

                await userManager.AddToRoleAsync(user, "Admin");

                // Create the associated UserProfile
                db.UserProfiles.Add(new UserProfile
                {
                    ApplicationUserId = user.Id
                });
            }

            await db.SaveChangesAsync();
        }
    }
}
