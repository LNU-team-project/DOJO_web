using DOJO2.Infrastructure.Services;
using DOJO2.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DOJO2.Controllers;

public class AccountController : Controller
{
    private const string BlockedNoticeCookieName = "dojo_blocked_notice";
    private const string AccountControllerName = "Account";
    private const string HomeControllerName = "Home";
    private const string DashboardActionName = "Dashboard";
    private const string BlockedUserMessage = "Ваш обліковий запис заблоковано. Зверніться до адміністратора.";

    private readonly IAuthService _authService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IAuthService authService,
        ILogger<AccountController> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null, bool blocked = false)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(DashboardActionName, HomeControllerName);
        }

        var hasBlockedCookieNotice = Request.Cookies.TryGetValue(BlockedNoticeCookieName, out var blockedCookieValue)
            && string.Equals(blockedCookieValue, "1", StringComparison.Ordinal);

        if (blocked || hasBlockedCookieNotice)
        {
            ViewData["BlockedMessage"] = BlockedUserMessage;
            Response.Cookies.Delete(BlockedNoticeCookieName);
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

        var result = await _authService.LoginAsync(model.Email.Trim(), model.Password, model.RememberMe);

        if (!result.Success)
        {
            if (string.Equals(result.Message, BlockedUserMessage, StringComparison.Ordinal))
            {
                ViewData["BlockedMessage"] = BlockedUserMessage;
            }

            ModelState.AddModelError(string.Empty, result.Message ?? "Помилка при вході");
            return View(model);
        }

        _logger.LogInformation("Користувач успішно увійшов");
        return RedirectToAction(DashboardActionName, HomeControllerName);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync();
        return RedirectToAction("Register", AccountControllerName);
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

        var result = await _authService.RegisterAsync(model.UserName.Trim(), model.Email.Trim(), model.Password);
        
        if (!result.Success)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
            return View(model);
        }

        _logger.LogInformation("Користувач успішно зареєстрований");
        return RedirectToAction(DashboardActionName, HomeControllerName);
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
            var callbackUrl = Url.Action("ResetPassword", AccountControllerName, new { code = "PLACEHOLDER" }, protocol: HttpContext.Request.Scheme);
            // Замінюємо PLACEHOLDER на реальний код у сервісі
            await _authService.ForgotPasswordAsync(model.Email ?? string.Empty, callbackUrl ?? string.Empty);
            
            // Завжди показуємо одну й ту ж сторінку, щоб не розкривати чи користувач існує
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

        var result = await _authService.ResetPasswordAsync(model.Email ?? string.Empty, model.Code ?? string.Empty, model.Password ?? string.Empty);
        
        if (result.Success)
        {
            _logger.LogInformation("Пароль успішно скинут для користувача");
            return RedirectToAction(nameof(ResetPasswordConfirmation), AccountControllerName);
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
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

        var result = await _authService.SendTestEmailAsync(email);
        return Content(result.Message ?? "Помилка при відправленні email");
    }

    [HttpPost]
    public async Task<IActionResult> SendEmailConfirmation()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new { success = false, message = "Користувача не знайдено" });
        }

        var userResult = await _authService.GetUserAsync(userId.ToString());
        if (!userResult.Success || userResult.Data == null)
        {
            return NotFound(new { success = false, message = "Користувача не знайдено" });
        }

        var user = userResult.Data;
        var callbackUrl = Url.Action(
            nameof(ConfirmEmail),
            AccountControllerName,
            new { userId = user.Id, code = "PLACEHOLDER" },
            protocol: Request.Scheme);

        var result = await _authService.SendEmailConfirmationAsync(user.Email ?? string.Empty, callbackUrl ?? string.Empty);

        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        TempData["EmailConfirmationSent"] = true;
        return Ok(new { success = true, message = result.Message });
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(int userId, string code)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.ConfirmEmailAsync(userId, code);
        
        if (result.Success)
        {
            _logger.LogInformation("Email підтверджено для користувача {UserId}", userId);
            return RedirectToAction(DashboardActionName, HomeControllerName, new { confirmed = true });
        }

        _logger.LogWarning("Не вдалося підтвердити email для користувача {UserId}", userId);
        return RedirectToAction("Login", AccountControllerName);
    }
}
