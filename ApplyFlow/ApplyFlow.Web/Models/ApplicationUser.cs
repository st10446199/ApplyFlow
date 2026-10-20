using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ApplyFlow.Web.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(80)]
        public string DisplayName { get; set; } = string.Empty;

        [StringLength(4000)]
        public string? Skills { get; set; }
    }
}