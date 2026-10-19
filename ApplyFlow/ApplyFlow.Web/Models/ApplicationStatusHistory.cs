using ApplyFlow.Web.Enums;

namespace ApplyFlow.Web.Models
{
    public class ApplicationStatusHistory
    {
        public int Id { get; set; }

        public int JobApplicationId { get; set; }

        public JobApplication JobApplication { get; set; } = null!;

        public ApplicationStatus? PreviousStatus { get; set; }

        public ApplicationStatus NewStatus { get; set; }

        public DateTimeOffset ChangedAt { get; set; }
    }
}