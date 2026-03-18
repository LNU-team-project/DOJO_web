using System.ComponentModel.DataAnnotations;

namespace DOJO2.Presentation.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Введіть адресу пошти.")]
    [EmailAddress(ErrorMessage = "Введіть коректну адресу пошти.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть пароль.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
