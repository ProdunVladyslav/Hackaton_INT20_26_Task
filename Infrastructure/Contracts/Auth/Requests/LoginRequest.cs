using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Contracts.Auth.Requests;

/// <summary>
/// Command accepted by POST /api/auth/login.
///
/// WHY a record:
///   Records are immutable by default and have value equality built in —
///   perfect for DTOs that carry data without behaviour.
///   'sealed' prevents accidental inheritance.
///
/// WHY [property: ...] target syntax:
///   In record primary constructors, attributes without a target go on the
///   constructor parameter, not the generated property. The 'property:' target
///   makes the annotation land on the property so ASP.NET Core's model
///   validator (triggered by [ApiController]) can see and enforce it.
/// </summary>
public sealed record LoginRequest(
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Must be a valid e-mail address.")]
    string Email,

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    string Password
);
