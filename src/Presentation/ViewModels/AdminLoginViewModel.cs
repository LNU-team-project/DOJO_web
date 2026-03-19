using System.ComponentModel.DataAnnotations;

namespace DOJO2.Presentation.ViewModels
{
    public class AdminLoginViewModel
    {
        [Required(ErrorMessage = "Ім'я користувача є обов'язковим")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Пароль є обов'язковим")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}