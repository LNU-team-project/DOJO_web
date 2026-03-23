using DOJO2.Domain.Entities;
using DOJO2.Presentation.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity.UI.Services;
using src.Presentation.ViewModels;
using System.Text.Encodings.Web;

namespace DOJO2.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ILogger<AccountController> _logger;
    private readonly IEmailSender _emailSender;

    public AccountController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ILogger<AccountController> logger,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _emailSender = emailSender;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Невірна пошта або пароль.");
            return View(model);
        }

        var signInResult = await _signInManager.PasswordSignInAsync(
            user.UserName ?? string.Empty,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (!signInResult.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Невірна пошта або пароль.");
            return View(model);
        }

        _logger.LogInformation("User logged in: {UserName}", user.UserName);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new AppUser
        {
            UserName = model.UserName.Trim(),
            Email = model.Email.Trim()
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        _logger.LogInformation("User created: {UserName}", user.UserName);

        return RedirectToAction("Index", "Home");
    }
    
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByEmailAsync(model.Email ?? string.Empty);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return View("ForgotPasswordConfirmation");
            }

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code }, protocol: HttpContext.Request.Scheme);

            await _emailSender.SendEmailAsync(
                model.Email ?? string.Empty,
                "Reset Password",
                $"Please reset your password by clicking here: <a href='{HtmlEncoder.Default.Encode(callbackUrl ?? string.Empty)}'>link</a>");

            return View("ForgotPasswordConfirmation");
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult ResetPassword(string? code = null)
    {
        return string.IsNullOrEmpty(code) ? View("Error") : View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        var user = await _userManager.FindByEmailAsync(model.Email ?? string.Empty);
        if (user == null)
        {
            // Don't reveal that the user does not exist
            return RedirectToAction(nameof(ResetPasswordConfirmation), "Account");
        }
        var result = await _userManager.ResetPasswordAsync(user, model.Code ?? string.Empty, model.Password ?? string.Empty);
        if (result.Succeeded)
        {
            return RedirectToAction(nameof(ResetPasswordConfirmation), "Account");
        }
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(model);
    }

    [HttpGet]
    public IActionResult ResetPasswordConfirmation()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> TestEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return Content("Будь ласка, надайте email адресу в query string, наприклад: /Account/TestEmail?email=your-email@example.com");
        }

        await _emailSender.SendEmailAsync(email, "SendGrid Test", "Це тестовий email з SendGrid.");
        return Content($"Тестовий email відправлено на {email}. Перевірте свою поштову скриньку.");
    }
}
