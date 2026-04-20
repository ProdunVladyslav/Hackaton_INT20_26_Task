using Infrastructure.Contracts.AIGeneration;
using Infrastructure.Contracts.AIGeneration.Requests;
using Infrastructure.Contracts.Edges.Requests;
using Infrastructure.Contracts.Flows.Requests;
using Infrastructure.Contracts.Nodes.Requests;
using Infrastructure.Contracts.Options.Requests;
using Infrastructure.Services.Implementations;
using Infrastructure.UseCases.Edges;
using Infrastructure.UseCases.Flows;
using Infrastructure.UseCases.Nodes;
using Infrastructure.UseCases.Options;
using System.Text.Json;

namespace Infrastructure.UseCases.AIGeneration
{
    public class GenerateFlowUseCase(
        CreateFlowUseCase createFlow,
        CreateNodeUseCase createNode,
        CreateEdgeUseCase createEdge,
        CreateOptionUseCase createOption,
        SetEntryNodeUseCase setEntryNode,
        ClaudeService claudeService)
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public async Task<Guid> ExecuteAsync(
            GenerateFlowRequest request,
            Guid applicationUserId,
            CancellationToken ct = default)
        {
            var raw = await claudeService.PromptAsync(request.UserPrompt, ct);
            var json = StripMarkdownFences(raw);

            GeneratedFlowPlan plan;
            try
            {
                plan = JsonSerializer.Deserialize<GeneratedFlowPlan>(json, JsonOptions)
                    ?? throw new InvalidOperationException("Claude returned an empty plan.");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Claude returned invalid or truncated JSON — try increasing MaxTokens. Details: {ex.Message}");
            }

            // ── 3. Create flow ────────────────────────────────────────────────
            var flowResult = await createFlow.ExecuteAsync(
                new CreateFlowRequest(plan.Flow.Name, plan.Flow.Description),
                applicationUserId, ct);

            flowResult.EnsureSuccess();
            var flowId = flowResult.Data.Id;

            // ── 4. Create nodes ───────────────────────────────────────────────
            var nodeIdMap = new Dictionary<string, Guid>(plan.Nodes.Count);

            foreach (var n in plan.Nodes)
            {
                var nodeResult = await createNode.ExecuteAsync(flowId, applicationUserId,
                    new CreateNodeRequest(
                        Type: n.Type,
                        Title: n.Title,
                        Description: n.Description,
                        MediaUrl: null,
                        PositionX: n.PositionX,
                        PositionY: n.PositionY,

                        // Question
                        AttributeKey: n.AttributeKey,
                        AnswerType: n.AnswerType,
                        ValueKind: n.ValueKind,
                        SliderMin: n.SliderMin,
                        SliderMax: n.SliderMax,

                        // Offer
                        Offer: n.Offer is null ? null : ToInlineOfferRequest(n.Offer),

                        // LeadCapture
                        IsRequired: n.IsRequired,
                        Fields: n.Fields?
                            .Select(f => new LeadCaptureFieldRequest(
                                FieldType: f.FieldType,
                                IsRequired: f.IsRequired,
                                DisplayOrder: f.DisplayOrder,
                                Placeholder: f.Placeholder))
                            .ToList(),

                        // Redirect
                        Tier: n.Tier,
                        RedirectUrl: n.RedirectUrl,
                        AutoRedirectAfterSeconds: n.AutoRedirectAfterSeconds,
                        Links: n.Links?
                            .Select(l => new NodeRedirectLinkRequest(l.Label, l.Url))
                            .ToList()),
                    ct);

                nodeResult.EnsureSuccess();
                nodeIdMap[n.TempId] = nodeResult.Data.Id;
            }

            // ── 5. Create options ─────────────────────────────────────────────
            foreach (var n in plan.Nodes.Where(n => n.Options.Count > 0))
            {
                var realNodeId = nodeIdMap[n.TempId];

                foreach (var opt in n.Options.OrderBy(o => o.DisplayOrder))
                {
                    var optResult = await createOption.ExecuteAsync(realNodeId, applicationUserId,
                        new CreateOptionRequest(
                            Label: opt.Label,
                            Value: opt.Value,
                            DisplayOrder: opt.DisplayOrder,
                            MediaUrl: null,
                            ScoreDelta: opt.ScoreDelta),
                        ct);

                    optResult.EnsureSuccess();
                }
            }

            // ── 6. Create edges ───────────────────────────────────────────────
            var seenEdgePairs = new HashSet<(Guid, Guid)>();

            foreach (var e in plan.Edges)
            {
                if (!nodeIdMap.TryGetValue(e.SourceTempId, out var sourceId))
                    throw new InvalidOperationException(
                        $"Edge references unknown source tempId '{e.SourceTempId}'.");

                if (!nodeIdMap.TryGetValue(e.TargetTempId, out var targetId))
                    throw new InvalidOperationException(
                        $"Edge references unknown target tempId '{e.TargetTempId}'.");

                if (!seenEdgePairs.Add((sourceId, targetId)))
                    continue;

                var edgeResult = await createEdge.ExecuteAsync(flowId, applicationUserId,
                    new CreateEdgeRequest(sourceId, targetId, e.Priority, e.ConditionsJson), ct);

                if (!edgeResult.Success && edgeResult.StatusCode == 409)
                    continue;

                edgeResult.EnsureSuccess();
            }

            // ── 7. Set entry node ─────────────────────────────────────────────
            if (!nodeIdMap.TryGetValue(plan.EntryNodeTempId, out var entryId))
                throw new InvalidOperationException(
                    $"Entry node tempId '{plan.EntryNodeTempId}' not found in node map.");

            var entryResult = await setEntryNode.ExecuteAsync(flowId, applicationUserId,
                new SetEntryNodeRequest(entryId), ct);

            entryResult.EnsureSuccess();

            return flowId;
        }

        private static InlineOfferRequest ToInlineOfferRequest(GeneratedOfferSpec o) =>
            new(Slug: o.Slug,
                Name: o.Name,
                Headline: o.Headline,
                Body: o.Body,
                ImageUrl: o.ImageUrl,
                CalendarUrl: o.CalendarUrl,
                CtaText: o.CtaText,
                CtaUrl: o.CtaUrl,
                Tier: o.Tier,
                CalendarProvider: o.CalendarProvider,
                IsPrimary: o.IsPrimary);

        private static string StripMarkdownFences(string raw)
        {
            var s = raw.Trim();
            if (!s.StartsWith("```")) return s;

            var firstNewline = s.IndexOf('\n');
            if (firstNewline < 0) return s;

            s = s[(firstNewline + 1)..];

            var closingFence = s.LastIndexOf("```");
            if (closingFence >= 0) s = s[..closingFence];

            return s.Trim();
        }
    }
}