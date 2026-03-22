using System.ComponentModel.DataAnnotations;

namespace src.Presentation.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}