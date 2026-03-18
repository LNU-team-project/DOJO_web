using System.ComponentModel.DataAnnotations;

namespace DOJO2.Presentation.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Введіть ім'я користувача.")]
    [MinLength(3, ErrorMessage = "Ім'я користувача має містити щонайменше 3 символи.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть адресу пошти.")]
    [EmailAddress(ErrorMessage = "Введіть коректну адресу пошти.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введіть пароль.")]
    [MinLength(6, ErrorMessage = "Пароль має містити щонайменше 6 символів.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Підтвердіть пароль.")]
    [Compare("Password", ErrorMessage = "Паролі не співпадають.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
