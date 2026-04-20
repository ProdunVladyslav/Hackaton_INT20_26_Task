using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts.Auth
{
    public sealed record MeResponse(
        /// <summary>The user's unique identifier (matches the JWT 'sub' claim).</summary>
        Guid UserId,

        /// <summary>The user's e-mail address.</summary>
        string Email,

        /// <summary>The user's login / display name.</summary>
        string UserName,

        /// <summary>The linked UserProfile row ID, or null if no profile exists yet.</summary>
        Guid? ProfileId
    );
}
