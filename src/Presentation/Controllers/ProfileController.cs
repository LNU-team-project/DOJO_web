using DOJO2.Application.Interfaces;
using DOJO2.Application.ViewModels;
using DOJO2.Application.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DOJO2.Controllers;

[Authorize]
public class ProfileController : BaseApiController
{
    private readonly IUserService _userService;

    public ProfileController(IUserService userService, ILogger<ProfileController> logger)
        : base(logger)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
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

        return ToActionResultWithNotFound(result);
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

        return ToActionResult(result);
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
        var uploadData = await BuildUploadDataAsync(avatar);
        var result = await _userService.UpdateUserAvatarAsync(userId, uploadData!);

        return ToActionResult(result);
    }

    private static async Task<FileUploadData?> BuildUploadDataAsync(IFormFile? file)
    {
        if (file == null)
        {
            return null;
        }

        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms);

        return new FileUploadData
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            Length = file.Length,
            Content = ms.ToArray()
        };
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

        return ToActionResult(result);
    }

    /// <summary>
    /// Оновлює аватар користувача
    /// </summary>
    [HttpPost("settings/avatar")]
    public async Task<IActionResult> UpdateAvatar([FromForm] IFormFile? avatar)
    {
        return await UploadAvatar(avatar);
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

        return Ok(new { success = true, message = "Посилання для скидання пароля відправлено" });
    }

    /// <summary>
    /// Видаляє акаунт поточного користувача (self-service)
    /// </summary>
    [HttpDelete("me")]
    public async Task<IActionResult> DeleteMyAccount()
    {
        var authError = ValidateUserAuthorization();
        if (authError != null)
        {
            return authError;
        }

        var userId = GetCurrentUserId() ?? 0;
        var result = await _userService.DeleteUserAccountAsync(userId);

        if (!result.Success)
        {
            return ToActionResult(result);
        }

        if (HttpContext.RequestServices != null)
        {
            await HttpContext.SignOutAsync();
        }
        else
        {
            _logger.LogWarning("Не вдалося виконати sign-out після видалення акаунта: RequestServices не налаштовано");
        }

        _logger.LogInformation("Користувач {UserId} видалив власний акаунт", userId);

        return ToActionResult(result);
    }
}
