using Infrastructure.Contracts.AIGeneration;
using Infrastructure.Contracts.AIGeneration.Requests;
using Infrastructure.Contracts.Edges.Requests;
using Infrastructure.Contracts.Edges.Responses;
using Infrastructure.Contracts.Flows.Requests;
using Infrastructure.Contracts.Flows.Responses;
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

        public async Task<Guid> ExecuteAsync(GenerateFlowRequest request, CancellationToken ct = default)
        {
            // ── 1. Ask Claude ────────────────────────────────────────────────────────
            var raw = await claudeService.PromptAsync(request.UserPrompt, ct);

            // ── 2. Strip markdown fences & parse ─────────────────────────────────────
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

            // ── 3. Create flow ───────────────────────────────────────────────────────
            FlowResult<FlowSummaryResponse> flowResult = await createFlow.ExecuteAsync(
                new CreateFlowRequest(plan.Flow.Name, plan.Flow.Description), ct);

            flowResult.EnsureSuccess();
            var flowId = flowResult.Data.Id;

            // ── 4. Create all nodes (positions included) ─────────────────────────────
            var nodeIdMap = new Dictionary<string, Guid>(plan.Nodes.Count);

            foreach (var n in plan.Nodes)
            {
                var nodeResult = await createNode.ExecuteAsync(flowId,
                    new CreateNodeRequest(
                        Type: n.Type,
                        Title: n.Title,
                        AttributeKey: n.AttributeKey,
                        Description: n.Description,
                        MediaUrl: null,
                        PositionX: n.PositionX,
                        PositionY: n.PositionY,
                        AnswerType: n.AnswerType,
                        ValueKind: n.ValueKind,
                        SliderMin: n.SliderMin,
                        SliderMax: n.SliderMax,
                        Offer: n.Offer is null ? null : ToInlineOfferRequest(n.Offer)),
                    ct);

                nodeResult.EnsureSuccess();
                nodeIdMap[n.TempId] = nodeResult.Data.Id;
            }

            // ── 5. Create options ────────────────────────────────────────────────────
            foreach (var n in plan.Nodes.Where(n => n.Options.Count > 0))
            {
                var realNodeId = nodeIdMap[n.TempId];

                foreach (var opt in n.Options.OrderBy(o => o.DisplayOrder))
                {
                    var optResult = await createOption.ExecuteAsync(realNodeId,
                        new CreateOptionRequest(opt.Label, opt.Value, opt.DisplayOrder), ct);

                    optResult.EnsureSuccess();
                }
            }

            // ── 6. Create edges ──────────────────────────────────────────────────────
            var seenEdgePairs = new HashSet<(Guid, Guid)>();

            foreach (var e in plan.Edges)
            {
                if (!nodeIdMap.TryGetValue(e.SourceTempId, out var sourceId))
                    throw new InvalidOperationException(
                        $"Edge references unknown source tempId '{e.SourceTempId}'.");

                if (!nodeIdMap.TryGetValue(e.TargetTempId, out var targetId))
                    throw new InvalidOperationException(
                        $"Edge references unknown target tempId '{e.TargetTempId}'.");

                // Client-side dedup — skip before even calling the API
                var pair = (sourceId, targetId);
                if (!seenEdgePairs.Add(pair))
                {
                    // Duplicate detected in the plan — skip silently
                    continue;
                }

                FlowResult<EdgeResponse> edgeResult = await createEdge.ExecuteAsync(flowId,
                    new CreateEdgeRequest(sourceId, targetId, e.Priority, e.ConditionsJson), ct);

                // 409 = edge already exists — not fatal, skip and continue
                if (!edgeResult.Success && edgeResult.StatusCode == 409)
                    continue;

                edgeResult.EnsureSuccess();
            }

            // ── 7. Set entry node ────────────────────────────────────────────────────
            if (!nodeIdMap.TryGetValue(plan.EntryNodeTempId, out var entryId))
                throw new InvalidOperationException(
                    $"Entry node tempId '{plan.EntryNodeTempId}' not found in node map.");

            FlowResult<FlowSummaryResponse> entryResult = await setEntryNode.ExecuteAsync(flowId,
                new SetEntryNodeRequest(entryId), ct);

            entryResult.EnsureSuccess();

            return flowId;
        }

        private static InlineOfferRequest ToInlineOfferRequest(GeneratedOfferSpec o) =>
            new(
                Slug: o.Slug,
                Name: o.Name,
                Description: o.Description,
                Duration: o.Duration,
                DigitalContent: o.DigitalContent,
                PhysicalWellnessKitName: o.PhysicalWellnessKitName,
                PhysicalWellnessKitItems: o.PhysicalWellnessKitItems,
                Price: o.Price,
                ImageUrl: o.ImageUrl,
                CtaText: o.CtaText,
                CtaUrl: o.CtaUrl,
                IsPrimary: o.IsPrimary);

        private static string StripMarkdownFences(string raw)
        {
            var s = raw.Trim();

            if (!s.StartsWith("```"))
                return s;

            // Remove opening fence (```json or just ```)
            var firstNewline = s.IndexOf('\n');
            if (firstNewline < 0)
                return s;

            s = s[(firstNewline + 1)..];

            // Remove closing fence
            var closingFence = s.LastIndexOf("```");
            if (closingFence >= 0)
                s = s[..closingFence];

            return s.Trim();
        }
    }
}
