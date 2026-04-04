using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Contracts.AIGeneration.Requests
{
    public record ClaudeRequest(string Role, string Content)
    {
        public static ClaudeRequest User(string content) => new("user", content);
        public static ClaudeRequest Assistant(string content) => new("assistant", content);
    }
}
