using Domain.Model.Auth;
using Domain.Model.Survey;

namespace Domain.Model.AdminProfile;

/// <summary>
/// The domain profile entity — everything about the user beyond "can they log in?".
/// </summary>
public class UserProfile
{
    /// <summary>Own surrogate PK. Lets you reference profiles independently of the user ID.</summary>
    public Guid Id { get; set; }

    /// <summary>
    /// FK to ApplicationUser.Id.
    /// </summary>
    public Guid ApplicationUserId { get; set; }

    /// <summary>Navigation property back to the owning user.</summary>
    public ApplicationUser ApplicationUser { get; set; } = null!;

    public ICollection<Flow> Flows { get; set; } = new List<Flow>();
    public ICollection<Offer> Offers { get; set; } = new List<Offer>();

    // ── Add hackathon-specific profile fields below as the task becomes clear ──
    // public string? DisplayName { get; set; }
    // public string? AvatarUrl   { get; set; }
    // public DateOnly? DateOfBirth { get; set; }
}
