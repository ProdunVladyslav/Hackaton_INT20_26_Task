using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Contracts.Auth.Responses
{
    public sealed record SignUpResult(
        bool Success,
        string? ErrorMessage = null,
        LoginResponse? User = null
    );
}
