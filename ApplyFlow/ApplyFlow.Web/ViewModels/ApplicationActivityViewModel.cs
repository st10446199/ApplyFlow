using System.ComponentModel.DataAnnotations;
using ApplyFlow.Web.Enums;
using ApplyFlow.Web.Models;

namespace ApplyFlow.Web.ViewModels
{
    public class ApplicationActivityViewModel : IValidatableObject
    {
        public int Id { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public string JobTitle { get; set; } = string.Empty;

        public ApplicationStatus Status { get; set; }

        [Display(Name = "Follow-up date")]
        [DataType(DataType.Date)]
        public DateOnly? FollowUpDate { get; set; }

        public List<ApplicationStatusHistory> History { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (FollowUpDate.HasValue
                && (FollowUpDate.Value.Year < 2000
                    || FollowUpDate.Value.Year > 2100))
            {
                yield return new ValidationResult(
                    "Choose a date between 2000 and 2100.",
                    new[] { nameof(FollowUpDate) });
            }
        }
    }
}