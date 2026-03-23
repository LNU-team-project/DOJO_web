﻿﻿using DOJO2.Infrastructure.Services;
using DOJO2.Presentation.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DOJO2.Controllers
{
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IAdminService adminService, ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _logger = logger;
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
    }
}