namespace Domain.Model.Survey
{
    public enum LeadCaptureFieldType
    {
        FullName,
        Email,
        Phone,
        CompanyName,
        JobTitle,
        CompanySize,
        Website
    }

    public sealed class NodeLeadCaptureField
    {
        public Guid Id { get; private set; }
        public Guid NodeLeadCaptureId { get; private set; }
        public LeadCaptureFieldType FieldType { get; private set; }
        public bool IsRequired { get; private set; }
        public int DisplayOrder { get; private set; }
        public string Placeholder { get; private set; }

        // Reserved attribute key injected into answerContext automatically.
        // Not editable — derived from FieldType.
        public string AttributeKey => FieldType switch
        {
            LeadCaptureFieldType.FullName => "lead_name",
            LeadCaptureFieldType.Email => "lead_email",
            LeadCaptureFieldType.Phone => "lead_phone",
            LeadCaptureFieldType.CompanyName => "lead_company",
            LeadCaptureFieldType.JobTitle => "lead_title",
            LeadCaptureFieldType.CompanySize => "lead_company_size",
            LeadCaptureFieldType.Website => "lead_website",
            _ => throw new InvalidOperationException("Unknown field type")
        };

        private NodeLeadCaptureField() { }

        public static NodeLeadCaptureField Create(
            Guid nodeLeadCaptureId,
            LeadCaptureFieldType fieldType,
            bool isRequired,
            int displayOrder,
            string placeholder = "")
        {
            if (fieldType == LeadCaptureFieldType.Email && !isRequired)
                throw new ArgumentException("Email field must always be required.");

            return new NodeLeadCaptureField
            {
                Id = Guid.NewGuid(),
                NodeLeadCaptureId = nodeLeadCaptureId,
                FieldType = fieldType,
                IsRequired = isRequired,
                DisplayOrder = displayOrder,
                Placeholder = placeholder
            };
        }

        public void SetPlaceholder(string value) => Placeholder = value ?? string.Empty;
        public void SetOrder(int order)
        {
            if (order < 0) throw new ArgumentException("Order cannot be negative.");
            DisplayOrder = order;
        }

        public void SetIsRequired(bool value) => IsRequired = value;
    }
}
