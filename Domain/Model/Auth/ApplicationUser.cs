using Domain.Model.AdminProfile;
using Microsoft.AspNetCore.Identity;

namespace Domain.Model.Auth;

/// <summary>
/// The identity entity for every person who can log in to the system.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    // Id (Guid) is inherited from IdentityUser<Guid> — no need to redeclare it.
    public UserProfile? Profile { get; set; }
}
