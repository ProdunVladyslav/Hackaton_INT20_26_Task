using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Model.Survey
{
    public sealed class NodeRedirectLink
    {
        public Guid Id { get; private set; }
        public Guid NodeRedirectId { get; private set; }
        public string Label { get; private set; }
        public string Url { get; private set; }
        public int DisplayOrder { get; private set; }

        private NodeRedirectLink() { }

        public static NodeRedirectLink Create(
            Guid nodeRedirectId, string label, string url, int displayOrder)
        {
            if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Label required.");
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("Url required.");

            return new NodeRedirectLink
            {
                Id = Guid.NewGuid(),
                NodeRedirectId = nodeRedirectId,
                Label = label,
                Url = url,
                DisplayOrder = displayOrder
            };
        }


        public void SetLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Label required.");
            Label = label;
        }

        public void SetUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("Url required.");
            Url = url;
        }

        public void SetDisplayOrder(int order) => DisplayOrder = order;
    }
}
