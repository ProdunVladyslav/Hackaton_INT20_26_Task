using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Contracts.AIGeneration.Responses
{
    public record ClaudeResponse(
        string Id,
        string Model,
        string StopReason,
        string Text,
        ClaudeUsage Usage
    );

    public record ClaudeUsage(int InputTokens, int OutputTokens);
}
