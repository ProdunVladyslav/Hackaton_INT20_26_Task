using Infrastructure.Contracts.AIGeneration.Requests;
using Infrastructure.Contracts.AIGeneration.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Interfaces
{
    public interface IClaudeService
    {
        /// <summary>Single-turn prompt with a system instruction.</summary>
        Task<string> PromptAsync(string userMessage, CancellationToken ct = default);

        /// <summary>Multi-turn conversation — pass the full message history.</summary>
        Task<ClaudeResponse> ChatAsync(IEnumerable<ClaudeRequest> messages, CancellationToken ct = default);

        /// <summary>Streaming prompt — yields text chunks as they arrive.</summary>
        IAsyncEnumerable<string> StreamAsync(string userMessage, CancellationToken ct = default);
    }
}
