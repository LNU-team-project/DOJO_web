﻿using System.ComponentModel.DataAnnotations;

namespace DOJO2.Presentation.ViewModels
{
    public class AdminLoginViewModel
    {
        [Required(ErrorMessage = "Логін є обов'язковим")]
        public string? Login { get; set; }

        [Required(ErrorMessage = "Пароль є обов'язковим")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }
    }
}

