using System.ComponentModel.DataAnnotations;
using ApplyFlow.Web.Enums;

namespace ApplyFlow.Web.ViewModels
{
    public class CreateApplicationViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Enter the company name.")]
        [StringLength(120)]
        [Display(Name = "Company name")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter the job title.")]
        [StringLength(150)]
        [Display(Name = "Job title")]
        public string JobTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a location or specify Remote.")]
        [StringLength(150)]
        public string Location { get; set; } = string.Empty;

        [Range(
            typeof(decimal),
            "0.01",
            "999999999.99",
            ErrorMessage = "Enter a salary greater than zero and below one billion.")]
        [Display(Name = "Salary amount")]
        public decimal? SalaryAmount { get; set; }

        [RegularExpression(
            @"^(ZAR|USD|GBP|EUR)$",
            ErrorMessage = "Choose a supported currency.")]
        public string? Currency { get; set; } = "ZAR";

        [RegularExpression(
            @"^(Hourly|Monthly|Yearly)$",
            ErrorMessage = "Choose a valid pay period.")]
        [Display(Name = "Pay period")]
        public string? PayPeriod { get; set; } = "Monthly";

        [StringLength(2048)]
        [Display(Name = "Job URL")]
        public string? JobUrl { get; set; }

        [Required(ErrorMessage = "Choose an application status.")]
        [EnumDataType(
            typeof(ApplicationStatus),
            ErrorMessage = "Choose a valid application status.")]
        public ApplicationStatus? Status { get; set; }
            = ApplicationStatus.Saved;

        [DataType(DataType.Date)]
        [Display(Name = "Application date")]
        public DateOnly? ApplicationDate { get; set; }

        [StringLength(5000)]
        public string? Notes { get; set; }

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (SalaryAmount.HasValue)
            {
                if (string.IsNullOrWhiteSpace(Currency))
                {
                    yield return new ValidationResult(
                        "Choose a currency when entering a salary.",
                        new[] { nameof(Currency) });
                }

                if (string.IsNullOrWhiteSpace(PayPeriod))
                {
                    yield return new ValidationResult(
                        "Choose a pay period when entering a salary.",
                        new[] { nameof(PayPeriod) });
                }
            }

            if (!string.IsNullOrWhiteSpace(JobUrl))
            {
                var isValidUrl = Uri.TryCreate(
                    JobUrl.Trim(),
                    UriKind.Absolute,
                    out var uri);

                if (!isValidUrl ||
                    (uri!.Scheme != Uri.UriSchemeHttp &&
                     uri.Scheme != Uri.UriSchemeHttps))
                {
                    yield return new ValidationResult(
                        "Enter a full job URL starting with http:// or https://.",
                        new[] { nameof(JobUrl) });
                }
            }
        }
    }
}