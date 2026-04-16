using DOJO2.Application.Common;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DOJO2.Controllers;

/// <summary>
/// Базовий контролер для всіх API контролерів
/// Містить загальну логіку для авторизації та обробки результатів
/// </summary>
[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected readonly ILogger<BaseApiController> _logger;

    protected BaseApiController(ILogger<BaseApiController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Отримує ідентифікатор поточного авторизованого користувача з claims
    /// </summary>
    /// <returns>ID користувача або null якщо не авторизований</returns>
    protected int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId)
            ? userId
            : null;
    }

    /// <summary>
    /// Перевіряє авторизацію користувача
    /// </summary>
    /// <returns>IActionResult з 401 Unauthorized якщо не авторизований, інакше null</returns>
    protected IActionResult? ValidateUserAuthorization()
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
    /// Конвертує успішний Result у IActionResult з кодом 200 OK
    /// </summary>
    protected IActionResult Ok<T>(Result<T> result)
    {
        if (!result.Success)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = result.Message,
                errors = result.Errors
            });
        }

        return Ok(new { success = true, message = result.Message, data = result.Data });
    }

    /// <summary>
    /// Конвертує Result без даних у IActionResult
    /// </summary>
    protected IActionResult Ok(Result result)
    {
        if (!result.Success)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                message = result.Message,
                errors = result.Errors
            });
        }

        return Ok(new { success = true, message = result.Message });
    }

    /// <summary>
    /// Конвертує неуспішний Result у IActionResult
    /// Рекомендується для фіналізованих операцій
    /// </summary>
    protected IActionResult BadRequest<T>(Result<T> result)
    {
        return BadRequest(new
        {
            success = false,
            message = result.Message,
            errors = result.Errors
        });
    }

    /// <summary>
    /// Конвертує неуспішний Result без даних у IActionResult
    /// </summary>
    protected IActionResult BadRequest(Result result)
    {
        return BadRequest(new
        {
            success = false,
            message = result.Message,
            errors = result.Errors
        });
    }

    /// <summary>
    /// Конвертує Result у IActionResult
    /// Автоматично обирає OK або BadRequest залежно від Success
    /// </summary>
    protected IActionResult ToActionResult<T>(Result<T> result)
    {
        return result.Success
            ? Ok(result)
            : BadRequest(result);
    }

    /// <summary>
    /// Конвертує Result без даних у IActionResult
    /// Автоматично обирає OK або BadRequest залежно від Success
    /// </summary>
    protected IActionResult ToActionResult(Result result)
    {
        return result.Success
            ? Ok(result)
            : BadRequest(result);
    }

    /// <summary>
    /// Конвертує Result у IActionResult з особливим обробленням для NotFound
    /// </summary>
    protected IActionResult ToActionResultWithNotFound<T>(Result<T> result)
    {
        if (!result.Success && result.Message?.Contains("не знайдено") == true)
        {
            return NotFound(new
            {
                success = false,
                message = result.Message,
                errors = result.Errors
            });
        }

        return result.Success
            ? Ok(result)
            : BadRequest(result);
    }
}

