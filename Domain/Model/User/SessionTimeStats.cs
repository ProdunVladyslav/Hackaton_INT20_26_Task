using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Model.User
{
    public sealed record SessionTimeStats
    {
        // Session level
        public TimeSpan? TotalSessionDuration { get; init; }   // StartedAt → CompletedAt
        public bool IsCompleted { get; init; }

        // Answer level
        public int TotalAnswers { get; init; }
        public TimeSpan AverageAnswerDuration { get; init; }
        public TimeSpan MinAnswerDuration { get; init; }
        public TimeSpan MaxAnswerDuration { get; init; }
        public TimeSpan TotalAnsweringTime { get; init; }      // sum of all answer durations

        // Idle time (session time minus time actually spent answering)
        public TimeSpan? IdleTime => TotalSessionDuration - TotalAnsweringTime;

        public static SessionTimeStats Compute(UserSession session, IReadOnlyList<UserAnswer> answers)
        {
            if (answers.Count == 0)
                return new SessionTimeStats { IsCompleted = session.Status == SessionStatus.Completed };

            var durations = answers
                .Select(a => a.UserAnswerDuration)
                .Where(d => d > TimeSpan.Zero)  // guard against clock skew
                .ToList();

            var totalAnsweringTime = durations.Aggregate(TimeSpan.Zero, (sum, d) => sum + d);

            return new SessionTimeStats
            {
                IsCompleted = session.Status == SessionStatus.Completed,
                TotalSessionDuration = session.CompletedAt.HasValue
                                            ? session.CompletedAt.Value - session.StartedAt
                                            : DateTime.UtcNow - session.StartedAt,

                TotalAnswers = answers.Count,
                TotalAnsweringTime = totalAnsweringTime,

                AverageAnswerDuration = durations.Count > 0
                                            ? totalAnsweringTime / durations.Count
                                            : TimeSpan.Zero,

                MinAnswerDuration = durations.Count > 0 ? durations.Min() : TimeSpan.Zero,
                MaxAnswerDuration = durations.Count > 0 ? durations.Max() : TimeSpan.Zero,
            };
        }
    }
}
