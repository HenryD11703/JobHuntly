using System;

namespace JobHuntly.Domain.Entities
{
    public class Interview
    {
        public Guid Id { get; set; }
        public Guid JobApplicationId { get; set; }
        public DateTime InterviewDate { get; set; }
        public string RoundType { get; set; } = string.Empty; // e.g., "HR Phone Screen", "Technical", "Cultural"
        public string? Notes { get; set; }

        public JobApplication JobApplication { get; set; } = null!;
    }
}
