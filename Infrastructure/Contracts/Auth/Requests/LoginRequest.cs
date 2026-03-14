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
    /// <summary>The user's registered e-mail address.</summary>
    [property: Required(ErrorMessage = "Email is required.")]
    [property: EmailAddress(ErrorMessage = "Must be a valid e-mail address.")]
    string Email,

    /// <summary>The user's password (min 8 characters).</summary>
    [property: Required(ErrorMessage = "Password is required.")]
    [property: MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    string Password
);
