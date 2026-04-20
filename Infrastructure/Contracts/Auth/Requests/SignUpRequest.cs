using Infrastructure.Contracts.Auth.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Contracts.Auth.Requests
{
    public sealed record SignUpRequest(string Email, string Password);
}
