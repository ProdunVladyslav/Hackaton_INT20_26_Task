using Infrastructure.Contracts.AIGeneration;
using Infrastructure.Contracts.AIGeneration.Internal;
using Infrastructure.Contracts.AIGeneration.Requests;
using Infrastructure.Contracts.AIGeneration.Responses;
using Infrastructure.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Services.Implementations
{
    public sealed class ClaudeService : IClaudeService
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
        };

        private readonly HttpClient _http;
        private readonly ClaudeOptions _opts;
        private readonly string _systemPrompt;
        private readonly ILogger<ClaudeService> _log;

        public ClaudeService(
            HttpClient http,
            IOptions<ClaudeOptions> opts,
            ILogger<ClaudeService> log)
        {
            _http = http;
            _opts = opts.Value;
            _log = log;

            var path = Path.Combine(AppContext.BaseDirectory, "Prompts", "survey_flow_prompt.txt");
            _systemPrompt = File.ReadAllText(path);

            _http.BaseAddress = new Uri(_opts.BaseUrl);
            _http.Timeout = _opts.Timeout;
            _http.DefaultRequestHeaders.Add("x-api-key", _opts.ApiKey);
            _http.DefaultRequestHeaders.Add("anthropic-version", _opts.ApiVersion);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        public async Task<string> PromptAsync(
            string userMessage,
            CancellationToken ct = default)
        {
            var response = await ChatAsync(
                [ClaudeRequest.User(userMessage)],
                ct
            );
            return response.Text;
        }

        public async Task<ClaudeResponse> ChatAsync(
            IEnumerable<ClaudeRequest> messages,
            CancellationToken ct = default)
        {
            var request = BuildRequest(messages, stream: false);

            _log.LogDebug("Claude request → model={Model} msgs={Count}",
                request.model, request.messages.Length);

            using var httpResponse = await PostAsync(request, ct);
            httpResponse.EnsureSuccessStatusCode();

            var api = await httpResponse.Content.ReadFromJsonAsync<ApiResponse>(JsonOpts, ct)
                      ?? throw new InvalidOperationException("Empty response from Claude API.");

            var text = api.content.FirstOrDefault(c => c.type == "text")?.text ?? string.Empty;

            _log.LogDebug("Claude response ← stopReason={Stop} in={In} out={Out}",
                api.stop_reason, api.usage.input_tokens, api.usage.output_tokens);

            return new ClaudeResponse(
                Id: api.id,
                Model: api.model,
                StopReason: api.stop_reason,
                Text: text,
                Usage: new ClaudeUsage(api.usage.input_tokens, api.usage.output_tokens)
            );
        }

        public async IAsyncEnumerable<string> StreamAsync(
            string userMessage,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var request = BuildRequest([ClaudeRequest.User(userMessage)], stream: true);
            var json = JsonSerializer.Serialize(request, JsonOpts);
            using var body = new StringContent(json, Encoding.UTF8, "application/json");

            // Use SendAsync with HttpCompletionOption.ResponseHeadersRead so we
            // can read the stream incrementally without buffering the whole body.
            using var req = new HttpRequestMessage(HttpMethod.Post, "/v1/messages") { Content = body };
            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:")) continue;

                var data = line["data:".Length..].Trim();
                if (data == "[DONE]") yield break;

                StreamEvent? ev;
                try { ev = JsonSerializer.Deserialize<StreamEvent>(data, JsonOpts); }
                catch (JsonException) { continue; }

                if (ev?.type == "content_block_delta" && ev.delta?.type == "text_delta")
                    yield return ev.delta.text ?? string.Empty;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private ApiRequest BuildRequest(
            IEnumerable<ClaudeRequest> messages,
            bool stream)
        {
            var apiMessages = messages
                .Select(m => new ApiMessage(m.Role, m.Content))
                .ToArray();

            return new ApiRequest(
                model: _opts.Model,
                max_tokens: _opts.MaxTokens,
                messages: apiMessages,
                system: _systemPrompt,
                temperature: _opts.Temperature,
                stream: stream ? true : null
            );
        }

        private Task<HttpResponseMessage> PostAsync(ApiRequest request, CancellationToken ct)
        {
            var json = JsonSerializer.Serialize(request, JsonOpts);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return _http.PostAsync("/v1/messages", content, ct);
        }
    }
}
