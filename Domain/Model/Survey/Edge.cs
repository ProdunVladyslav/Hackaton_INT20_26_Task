using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Model.Survey
{
    public class Edge
    {
        public Guid Id { get; private set; }
        public Guid FlowId { get; private set; }
        public Guid SourceNodeId { get; private set; }
        public Guid TargetNodeId { get; private set; }
        public int Priority { get; private set; }
        public string ConditionsJson { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private Edge() { }

        private Edge(Guid flowId, Guid source, Guid target, int priority, string conditions)
        {
            if (source == target)
                throw new ArgumentException("Source and target cannot be same");

            Id = Guid.NewGuid();
            FlowId = flowId;
            SourceNodeId = source;
            TargetNodeId = target;

            SetPriority(priority);
            ConditionsJson = conditions;

            CreatedAt = DateTime.UtcNow;
        }

        public static Edge Create(
            Guid flowId,
            Guid source,
            Guid target,
            int priority,
            string conditions)
        {
            return new Edge(flowId, source, target, priority, conditions);
        }

        public void SetPriority(int priority)
        {
            if (priority < 0)
                throw new ArgumentException("Priority cannot be negative");

            Priority = priority;
        }

        public void UpdateConditions(string json)
        {
            ConditionsJson = json;
        }
    }
}
