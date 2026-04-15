﻿﻿using DOJO2.Infrastructure.Services;
using DOJO2.Presentation.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DOJO2.Controllers
{
    public class AdminController : Controller
    {
        private const string SuccessMessageTempDataKey = "AdminUsersSuccessMessage";
        private const string ErrorMessageTempDataKey = "AdminUsersErrorMessage";

        private readonly IAdminService _adminService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IAdminService adminService, ILogger<AdminController> logger)
        {
            _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new AdminLoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AdminLoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _adminService.AuthenticateAdminAsync(model.Login ?? string.Empty, model.Password ?? string.Empty);
                
                if (result.Success)
                {
                    _logger.LogInformation("Адміністратор успішно увійшов: {Login}", model.Login);
                    return RedirectToAction("LoginSuccess");
                }

                ModelState.AddModelError(string.Empty, result.Message ?? "Помилка при аутентифікації");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult LoginSuccess()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Users(string? search)
        {
            var result = await _adminService.GetUsersAsync(search);
            if (!result.Success || result.Data == null)
            {
                _logger.LogWarning("Не вдалося завантажити список користувачів для адмін-панелі");
                ModelState.AddModelError(string.Empty, result.Message ?? "Не вдалося завантажити користувачів");
            }

            var vm = new AdminUsersPageViewModel
            {
                Search = search?.Trim() ?? string.Empty,
                Users = result.Data ?? new List<AdminUserListItemViewModel>()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockUser(int userId, string? search)
        {
            if (!ModelState.IsValid)
            {
                TempData[ErrorMessageTempDataKey] = "Невірні параметри запиту";
                return RedirectToAction(nameof(Users), new { search });
            }

            var result = await _adminService.BlockUserAsync(userId);
            if (result.Success)
            {
                TempData[SuccessMessageTempDataKey] = result.Message ?? "Користувача успішно заблоковано";
            }
            else
            {
                TempData[ErrorMessageTempDataKey] = result.Message ?? "Не вдалося заблокувати користувача";
            }

            return RedirectToAction(nameof(Users), new { search });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnblockUser(int userId, string? search)
        {
            if (!ModelState.IsValid)
            {
                TempData[ErrorMessageTempDataKey] = "Невірні параметри запиту";
                return RedirectToAction(nameof(Users), new { search });
            }

            var result = await _adminService.UnblockUserAsync(userId);
            if (result.Success)
            {
                TempData[SuccessMessageTempDataKey] = result.Message ?? "Користувача успішно розблоковано";
            }
            else
            {
                TempData[ErrorMessageTempDataKey] = result.Message ?? "Не вдалося розблокувати користувача";
            }

            return RedirectToAction(nameof(Users), new { search });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int userId, string? search)
        {
            if (!ModelState.IsValid)
            {
                TempData[ErrorMessageTempDataKey] = "Невірні параметри запиту";
                return RedirectToAction(nameof(Users), new { search });
            }

            var result = await _adminService.DeleteUserAsync(userId);
            if (result.Success)
            {
                TempData[SuccessMessageTempDataKey] = result.Message ?? "Користувача успішно видалено";
            }
            else
            {
                TempData[ErrorMessageTempDataKey] = result.Message ?? "Не вдалося видалити користувача";
            }

            return RedirectToAction(nameof(Users), new { search });
        }
    }
}