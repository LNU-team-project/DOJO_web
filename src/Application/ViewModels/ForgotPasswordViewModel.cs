using System.ComponentModel.DataAnnotations;

namespace DOJO2.Application.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; }
    }
}