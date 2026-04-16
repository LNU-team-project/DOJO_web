using System.ComponentModel.DataAnnotations;

namespace DOJO2.Application.ViewModels;

public class UserProfileViewModel
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public int ExpPoints { get; set; }
    public int Level { get; set; }
    public int CurrentStreak { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool EmailConfirmed { get; set; }
}

public class UpdateUserProfileViewModel
{
    [StringLength(256, MinimumLength = 3, ErrorMessage = "Ім'я користувача повинне мати від 3 до 256 символів")]
    public string? UserName { get; set; }

    [Phone(ErrorMessage = "Введіть корректний номер телефону")]
    public string? PhoneNumber { get; set; }
}
