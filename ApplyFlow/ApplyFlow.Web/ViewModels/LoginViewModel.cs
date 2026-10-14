using System.ComponentModel.DataAnnotations;

namespace ApplyFlow.Web.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Enter your email address.")]
        [EmailAddress]
        [StringLength(256)]
        [Display(Name = "Email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your password.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}