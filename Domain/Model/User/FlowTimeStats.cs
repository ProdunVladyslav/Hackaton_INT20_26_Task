using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Model.User
{
    public sealed record FlowTimeStats
    {
        // Session duration stats (StartedAt → CompletedAt)
        public TimeSpan AverageSessionDuration { get; init; }
        public TimeSpan MinSessionDuration { get; init; }
        public TimeSpan MaxSessionDuration { get; init; }
        public TimeSpan MedianSessionDuration { get; init; }

        // Answer duration stats across all sessions
        public TimeSpan AverageAnswerDuration { get; init; }
        public TimeSpan MinAnswerDuration { get; init; }
        public TimeSpan MaxAnswerDuration { get; init; }
        public TimeSpan MedianAnswerDuration { get; init; }

        // Per-node average answer time (for heatmap / slow-node detection)
        public IReadOnlyDictionary<Guid, TimeSpan> AverageAnswerDurationByNode { get; init; }
            = new Dictionary<Guid, TimeSpan>();

        public static FlowTimeStats Compute(
            IReadOnlyList<UserSession> sessions,
            IReadOnlyList<UserAnswer> answers)
        {
            // ── Session durations (completed only — others have no CompletedAt) ──
            var sessionDurations = sessions
                .Where(s => s.Status == SessionStatus.Completed && s.CompletedAt.HasValue)
                .Select(s => s.CompletedAt!.Value - s.StartedAt)
                .Where(d => d > TimeSpan.Zero)
                .OrderBy(d => d)
                .ToList();

            // ── Answer durations ──────────────────────────────────────────────
            var answerDurations = answers
                .Select(a => a.UserAnswerDuration)
                .Where(d => d > TimeSpan.Zero)
                .OrderBy(d => d)
                .ToList();

            // ── Per-node average ──────────────────────────────────────────────
            var byNode = answers
                .Where(a => a.UserAnswerDuration > TimeSpan.Zero)
                .GroupBy(a => a.NodeId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var durations = g.Select(a => a.UserAnswerDuration).ToList();
                        return TimeSpan.FromTicks((long)durations.Average(d => d.Ticks));
                    }
                );

            return new FlowTimeStats
            {
                // Session
                AverageSessionDuration = sessionDurations.Count > 0
                    ? TimeSpan.FromTicks((long)sessionDurations.Average(d => d.Ticks))
                    : TimeSpan.Zero,
                MinSessionDuration = sessionDurations.Count > 0 ? sessionDurations.First() : TimeSpan.Zero,
                MaxSessionDuration = sessionDurations.Count > 0 ? sessionDurations.Last() : TimeSpan.Zero,
                MedianSessionDuration = sessionDurations.Count > 0 ? Median(sessionDurations) : TimeSpan.Zero,

                // Answers
                AverageAnswerDuration = answerDurations.Count > 0
                    ? TimeSpan.FromTicks((long)answerDurations.Average(d => d.Ticks))
                    : TimeSpan.Zero,
                MinAnswerDuration = answerDurations.Count > 0 ? answerDurations.First() : TimeSpan.Zero,
                MaxAnswerDuration = answerDurations.Count > 0 ? answerDurations.Last() : TimeSpan.Zero,
                MedianAnswerDuration = answerDurations.Count > 0 ? Median(answerDurations) : TimeSpan.Zero,

                AverageAnswerDurationByNode = byNode,
            };
        }

        private static TimeSpan Median(List<TimeSpan> sorted)
        {
            int mid = sorted.Count / 2;
            return sorted.Count % 2 == 0
                ? TimeSpan.FromTicks((sorted[mid - 1].Ticks + sorted[mid].Ticks) / 2)
                : sorted[mid];
        }
    }
}
