using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Contracts.AIGeneration
{
    public sealed class ClaudeOptions
    {
        public const string Section = "Claude";

        /// <summary>Your Anthropic API key (sk-ant-…). Required.</summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>Model to use. Defaults to claude-sonnet-4-20250514.</summary>
        public string Model { get; set; } = "claude-sonnet-4-20250514";

        /// <summary>Hard cap on tokens in the response.</summary>
        public int MaxTokens { get; set; } = 1024;

        /// <summary>Sampling temperature 0–1. Null = API default.</summary>
        public double? Temperature { get; set; }

        /// <summary>Base URL — override only for proxies / testing.</summary>
        public string BaseUrl { get; set; } = "https://api.anthropic.com";

        /// <summary>Anthropic API version header.</summary>
        public string ApiVersion { get; set; } = "2023-06-01";

        /// <summary>HTTP timeout for non-streaming requests.</summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(300);
    }
}
