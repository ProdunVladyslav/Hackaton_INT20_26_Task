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
        public Guid CurrentNodeId { get; private set; }
        public SessionStatus Status { get; private set; }
        public string UtmSource { get; private set; }
        public string UtmCampaign { get; private set; }
        public DateTime StartedAt { get; private set; }
        public DateTime? CompletedAt { get; private set; }

        private UserSession() { }

        private UserSession(Guid flowId, Guid entryNodeId)
        {
            Id = Guid.NewGuid();
            FlowId = flowId;
            CurrentNodeId = entryNodeId;
            Status = SessionStatus.InProgress;
            StartedAt = DateTime.UtcNow;
        }

        public static UserSession Create(Guid flowId, Guid entryNodeId)
        {
            return new UserSession(flowId, entryNodeId);
        }

        public void MoveToNode(Guid nodeId)
        {
            if (Status != SessionStatus.InProgress)
                throw new InvalidOperationException("Session is not active");

            CurrentNodeId = nodeId;
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
