using DOJO2.Infrastructure.Services;
using DOJO2.Presentation.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DOJO2.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(IUserService userService, ILogger<ProfileController> logger)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Отримує ідентифікатор поточного користувача з claims
    /// </summary>
    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId)
            ? userId
            : null;
    }

    /// <summary>
    /// Перевіряє авторизацію користувача
    /// </summary>
    private IActionResult? ValidateUserAuthorization()
    {
        var userId = GetCurrentUserId();
        if (userId == null || userId <= 0)
        {
            _logger.LogWarning("Невалідний userId або користувач не авторизований");
            return Unauthorized(new { success = false, message = "Користувача не знайдено" });
        }

        return null;
    }

    /// <summary>
    /// Отримує профіль поточного користувача
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var authError = ValidateUserAuthorization();
        if (authError != null)
        {
            return authError;
        }

        var userId = GetCurrentUserId() ?? 0;
        var result = await _userService.GetUserProfileAsync(userId);

        if (!result.Success)
        {
            return NotFound(new { success = false, message = result.Message, errors = result.Errors });
        }

        return Ok(new { success = true, message = result.Message, data = result.Data });
    }

    /// <summary>
    /// Оновлює профіль поточного користувача
    /// </summary>
    [HttpPut("update")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileViewModel? model)
    {
        if (model == null)
        {
            return BadRequest(new { success = false, message = "Модель профіля не може бути порожною" });
        }

        var authError = ValidateUserAuthorization();
        if (authError != null)
        {
            return authError;
        }

        var userId = GetCurrentUserId() ?? 0;
        var result = await _userService.UpdateUserProfileAsync(userId, model);

        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message, errors = result.Errors });
        }

        return Ok(new { success = true, message = result.Message, data = result.Data });
    }

    /// <summary>
    /// Завантажує новий аватар для поточного користувача
    /// </summary>
    [HttpPost("avatar")]
    public async Task<IActionResult> UploadAvatar([FromForm] IFormFile? avatar)
    {
        var authError = ValidateUserAuthorization();
        if (authError != null)
        {
            return authError;
        }

        if (avatar == null)
        {
            return BadRequest(new { success = false, message = "Виберіть файл аватара" });
        }

        var userId = GetCurrentUserId() ?? 0;
        var result = await _userService.UpdateUserAvatarAsync(userId, avatar);

        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message, errors = result.Errors });
        }

        return Ok(new { success = true, message = result.Message });
    }

    /// <summary>
    /// Вихід користувача з системи
    /// </summary>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        var authError = ValidateUserAuthorization();
        if (authError != null)
        {
            return authError;
        }

        _logger.LogInformation("Користувач {UserId} вийшов з системи", GetCurrentUserId());
        return Ok(new { success = true, message = "Вихід успішно виконано" });
    }

    /// <summary>
    /// Оновлює ім'я користувача
    /// </summary>
    [HttpPost("settings/username")]
    public async Task<IActionResult> UpdateUserName([FromBody] UpdateUserProfileViewModel? model)
    {
        var authError = ValidateUserAuthorization();
        if (authError != null)
        {
            return authError;
        }

        if (model == null || string.IsNullOrWhiteSpace(model.UserName))
        {
            return BadRequest(new { success = false, message = "Ім'я користувача не може бути порожнім" });
        }

        var userId = GetCurrentUserId() ?? 0;
        var result = await _userService.UpdateUserProfileAsync(userId, new UpdateUserProfileViewModel
        {
            UserName = model.UserName
        });

        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message, errors = result.Errors });
        }

        return Ok(new { success = true, message = result.Message, data = result.Data });
    }

    /// <summary>
    /// Оновлює аватар користувача
    /// </summary>
    [HttpPost("settings/avatar")]
    public async Task<IActionResult> UpdateAvatar([FromForm] IFormFile? avatar)
    {
        var authError = ValidateUserAuthorization();
        if (authError != null)
        {
            return authError;
        }

        if (avatar == null)
        {
            return BadRequest(new { success = false, message = "Виберіть файл аватара" });
        }

        var userId = GetCurrentUserId() ?? 0;
        var result = await _userService.UpdateUserAvatarAsync(userId, avatar);

        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message, errors = result.Errors });
        }

        return Ok(new { success = true, message = result.Message });
    }

    /// <summary>
    /// Надсилає посилання для скидання паролю
    /// </summary>
    [HttpPost("settings/password-reset-link")]
    public async Task<IActionResult> SendPasswordResetLink()
    {
        var authError = ValidateUserAuthorization();
        if (authError != null)
        {
            return authError;
        }

        var userId = GetCurrentUserId() ?? 0;
        var user = await _userService.GetUserProfileAsync(userId);
        if (!user.Success || string.IsNullOrWhiteSpace(user.Data?.Email))
        {
            return BadRequest(new { success = false, message = "Не вдалося отримати email користувача" });
        }

        // Реюз логіку з AccountController ForgotPassword через редирект/URL
        var resetUrl = Url.Action("ForgotPassword", "Account", null, Request.Scheme);
        return Ok(new { success = true, message = "Перейдіть для скидання паролю", resetUrl });
    }
}
