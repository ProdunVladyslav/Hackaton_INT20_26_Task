using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Model.Survey
{
    public class Option
    {
        public Guid Id { get; private set; }
        public Guid NodeId { get; private set; }
        public string Label { get; private set; }
        public string Value { get; private set; }
        public int DisplayOrder { get; private set; }
        public string? MediaUrl { get; private set; }

        /// <summary>
        /// Score contribution when this option is selected.
        /// Positive = more qualified, negative = less qualified.
        /// Zero (default) = neutral, no effect on score.
        /// Used by SubmitAnswerUseCase to accumulate session score,
        /// which is then available as __score__ in edge conditions.
        /// </summary>
        public int ScoreDelta { get; private set; }

        private Option() { }

        private Option(Guid nodeId, string label, string value, int order)
        {
            Id = Guid.NewGuid();
            NodeId = nodeId;

            SetLabel(label);
            SetValue(value);
            SetDisplayOrder(order);
            ScoreDelta = 0;
        }

        public static Option Create(Guid nodeId, string label, string value, int order)
            => new Option(nodeId, label, value, order);

        public void SetLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
                throw new ArgumentException("Option label required");
            Label = label;
        }

        public void SetValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Option value required");
            Value = value;
        }

        public void SetDisplayOrder(int order)
        {
            if (order < 0)
                throw new ArgumentException("Display order cannot be negative");
            DisplayOrder = order;
        }

        public void SetMedia(string url) => MediaUrl = url;

        /// <summary>
        /// Sets the score weight for this option.
        /// No bounds enforced at domain level — the builder decides the scale.
        /// Typical convention: +10/+20/+30 for positive signals, -10/-20 for negative.
        /// </summary>
        public void SetScoreDelta(int delta) => ScoreDelta = delta;
    }
}
