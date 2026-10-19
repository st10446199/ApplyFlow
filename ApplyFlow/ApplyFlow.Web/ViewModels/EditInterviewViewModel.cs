using System.ComponentModel.DataAnnotations;

namespace ApplyFlow.Web.ViewModels
{
    public class EditInterviewViewModel : IValidatableObject
    {
        public int Id { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public string JobTitle { get; set; } = string.Empty;

        [Display(Name = "Interview date and time")]
        public DateTime? InterviewLocalTime { get; set; }

        [Display(Name = "Remove the scheduled interview")]
        public bool RemoveInterview { get; set; }

        public bool HasScheduledInterview { get; set; }

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (RemoveInterview)
            {
                yield break;
            }

            if (!InterviewLocalTime.HasValue)
            {
                yield return new ValidationResult(
                    "Enter an interview date and time.",
                    new[] { nameof(InterviewLocalTime) });
            }
            else if (InterviewLocalTime.Value.Year < 2000
                || InterviewLocalTime.Value.Year > 2100)
            {
                yield return new ValidationResult(
                    "Choose a date between 2000 and 2100.",
                    new[] { nameof(InterviewLocalTime) });
            }
        }
    }
}