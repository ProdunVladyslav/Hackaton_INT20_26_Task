using Domain.Model.Survey;
using Domain.Services;

namespace Domain.Model.User
{
    public enum LeadStatus
    {
        New,
        Contacted,
        Booked,
        Closed,
        Rejected
    }

    public enum LeadType
    {
        Qualified,
        Disqualified
    }

    public sealed class Lead
    {
        public Guid Id { get; private set; }
        public Guid SessionId { get; private set; }
        public Guid FlowId { get; private set; }
        public Guid FlowOwnerId { get; private set; }

        // Identity — populated from UserAnswer rows with reserved keys
        public string? FullName { get; private set; }
        public string Email { get; private set; }
        public string? Phone { get; private set; }
        public string? CompanyName { get; private set; }
        public string? JobTitle { get; private set; }
        public string? CompanySize { get; private set; }
        public string? Website { get; private set; }

        // Qualification
        public int Score { get; private set; }
        public QualificationTier? Tier { get; private set; }
        public string? DisqualificationReason { get; private set; }
        public LeadType leadType { get; private set; }

        // Which terminal node they reached
        public Guid TerminalNodeId { get; private set; }
        public NodeType TerminalNodeType { get; private set; }  // Offer or Redirect

        // Which shared link brought this respondent in, if any
        public Guid? LeadChannelId { get; private set; }

        // Sales rep workflow
        public LeadStatus Status { get; private set; }
        public string? Notes { get; private set; }
        public Guid? AssignedToId { get; private set; }

        // Timing
        public DateTime CreatedAt { get; private set; }
        public int TimeToCompleteSeconds { get; private set; }

        private Lead() { }

        public static Lead CreateDisqualified(
            Guid sessionId,
            Guid flowId,
            Guid flowOwnerId,
            string email,
            int score,
            string? disqualificationReason,
            Guid terminalNodeId,
            LeadType leadType,
            NodeType terminalNodeType,
            int timeToCompleteSeconds,
            IDateTimeProvider time,
            Guid? leadChannelId = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email required to create a lead.");

            return new Lead
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                FlowId = flowId,
                FlowOwnerId = flowOwnerId,
                Email = email.ToLowerInvariant().Trim(),
                Score = score,
                DisqualificationReason = disqualificationReason,
                TerminalNodeId = terminalNodeId,
                TerminalNodeType = terminalNodeType,
                leadType = leadType,
                Status = LeadStatus.New,
                TimeToCompleteSeconds = timeToCompleteSeconds,
                CreatedAt = time.UtcNow,
                LeadChannelId = leadChannelId
            };
        }

        public static Lead CreateQualified(
            Guid sessionId,
            Guid flowId,
            Guid flowOwnerId,
            string email,
            int score,
            Guid terminalNodeId,
            LeadType leadType,
            QualificationTier qualificationTier,
            NodeType terminalNodeType,
            int timeToCompleteSeconds,
            IDateTimeProvider time,
            Guid? leadChannelId = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email required to create a lead.");

            return new Lead
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                FlowId = flowId,
                FlowOwnerId = flowOwnerId,
                Email = email.ToLowerInvariant().Trim(),
                Score = score,
                TerminalNodeId = terminalNodeId,
                TerminalNodeType = terminalNodeType,
                leadType = leadType,
                Tier = qualificationTier,
                Status = LeadStatus.New,
                TimeToCompleteSeconds = timeToCompleteSeconds,
                CreatedAt = time.UtcNow,
                LeadChannelId = leadChannelId
            };
        }


        public void SetIdentity(
            string? fullName, string? phone, string? company,
            string? jobTitle, string? companySize, string? website)
        {
            FullName = fullName;
            Phone = phone;
            CompanyName = company;
            JobTitle = jobTitle;
            CompanySize = companySize;
            Website = website;
        }

        public void SetStatus(LeadStatus status) => Status = status;
        public void SetNotes(string? notes) => Notes = notes;
        public void AssignTo(Guid? userId) => AssignedToId = userId;

        // Called by sales rep after reviewing
        public void UpdateTier(QualificationTier tier) => Tier = tier;
    }
}
