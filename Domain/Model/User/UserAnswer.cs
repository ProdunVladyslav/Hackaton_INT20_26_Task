using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Model.User
{
    public class UserAnswer
    {
        public Guid Id { get; private set; }
        public Guid SessionId { get; private set; }
        public Guid NodeId { get; private set; }
        public string AttributeKey { get; private set; }
        public string Value { get; private set; }
        public TimeSpan UserAnswerDuration { get; private set; }
        public DateTime AnsweredAt { get; private set; }

        private UserAnswer() { }

        private UserAnswer(Guid sessionId, Guid nodeId, string key, string value, DateTime lastAnswerAt)
        {
            Id = Guid.NewGuid();
            SessionId = sessionId;
            NodeId = nodeId;

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Attribute key required");

            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value required");

            AttributeKey = key;
            Value = value;

            AnsweredAt = DateTime.UtcNow;
            UserAnswerDuration = AnsweredAt - lastAnswerAt;
        }

        public static UserAnswer Create(Guid sessionId, Guid nodeId, string key, string value, DateTime lastAnswerAt)
        {
            return new UserAnswer(sessionId, nodeId, key, value, lastAnswerAt);
        }
    }
}
