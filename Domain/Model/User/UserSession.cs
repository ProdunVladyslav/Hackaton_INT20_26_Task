using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Model.User
{
    public enum SessionStatus
    {
        InProgress,
        Completed,
        Abandoned
    }

    public sealed class UserSession
    {
        public Guid Id { get; private set; }
        public Guid FlowId { get; private set; }
        public Guid? CurrentNodeId { get; private set; }
        public SessionStatus Status { get; private set; }
        public string UtmSource { get; private set; }
        public string UtmCampaign { get; private set; }
        public DateTime StartedAt { get; private set; }
        public DateTime? CompletedAt { get; private set; }
        public string? UserNodePath { get; private set; }



        /// <summary>
        /// Accumulated qualification score.
        /// Incremented by Option.ScoreDelta on each answer submission.
        /// Decremented by the same amount on go-back.
        /// Available as __score__ in edge conditions.
        /// </summary>
        public int Score { get; private set; }

        public List<UserAnswer> Answers { get; private set; } = new();

        private UserSession() { }

        private UserSession(Guid flowId, Guid entryNodeId)
        {
            Id = Guid.NewGuid();
            FlowId = flowId;
            CurrentNodeId = entryNodeId;
            Status = SessionStatus.InProgress;
            Score = 0;
            StartedAt = DateTime.UtcNow;
        }

        public static UserSession Create(Guid flowId, Guid entryNodeId)
            => new UserSession(flowId, entryNodeId);

        /// <summary>
        /// Applies a score delta. Pass negative value to reverse (used on go-back).
        /// </summary>
        public void AddScore(int delta) => Score += delta;

        public UserAnswer RecordAnswer(Guid nodeId, string key, string value)
        {
            if (Status != SessionStatus.InProgress)
                throw new InvalidOperationException("Cannot record answer on inactive session");

            var lastAnswerAt = Answers.Count > 0
                ? Answers.Max(a => a.AnsweredAt)
                : StartedAt;

            var answer = UserAnswer.Create(Id, nodeId, key, value, lastAnswerAt);
            Answers.Add(answer);
            return answer;
        }

        public void MoveToNode(Guid nodeId)
        {
            if (Status != SessionStatus.InProgress)
                throw new InvalidOperationException("Session is not active");

            if (UserNodePath == null)
                UserNodePath = $"{CurrentNodeId};{nodeId};";
            else
                UserNodePath += $"{nodeId};";

            CurrentNodeId = nodeId;
        }

        public void ClearCurrentNode()
        {
            CurrentNodeId = null;
            // optionally: Status = SessionStatus.Interrupted; etc.
        }

        public void Complete()
        {
            Status = SessionStatus.Completed;
            CompletedAt = DateTime.UtcNow;
        }

        public void Abandon()
        {
            Status = SessionStatus.Abandoned;
        }

        public void SetUtm(string? utmSource, string? utmCampaign)
        {
            UtmSource = utmSource ?? string.Empty;
            UtmCampaign = utmCampaign ?? string.Empty;
        }
    }
}
