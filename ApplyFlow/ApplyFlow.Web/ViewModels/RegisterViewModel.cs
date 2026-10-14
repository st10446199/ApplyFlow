using System.ComponentModel.DataAnnotations;

namespace ApplyFlow.Web.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Enter your name.")]
        [StringLength(80)]
        [Display(Name = "Display name")]
        public string DisplayName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your email address.")]
        [EmailAddress]
        [StringLength(256)]
        [Display(Name = "Email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a password.")]
        [StringLength(
            128,
            MinimumLength = 10,
            ErrorMessage = "Use between 10 and 128 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm your password.")]
        [DataType(DataType.Password)]
        [Compare(
            nameof(Password),
            ErrorMessage = "The passwords do not match.")]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}