using System.ComponentModel.DataAnnotations;

namespace ApplyFlow.Web.ViewModels
{
    public class SkillsProfileViewModel : IValidatableObject
    {
        [Display(Name = "Your skills")]
        [StringLength(
            4000,
            ErrorMessage = "Keep your skills list within 4,000 characters.")]
        public string? Skills { get; set; }

        public List<string> GetSkills()
        {
            return (Skills ?? string.Empty)
                .Split(
                    new[] { '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(skill => skill.Trim())
                .Where(skill => skill.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            var skills = GetSkills();

            if (skills.Count > 50)
            {
                yield return new ValidationResult(
                    "Enter no more than 50 different skills.",
                    new[] { nameof(Skills) });
            }

            if (skills.Any(skill => skill.Length > 80))
            {
                yield return new ValidationResult(
                    "Keep each skill within 80 characters.",
                    new[] { nameof(Skills) });
            }
        }
    }
}