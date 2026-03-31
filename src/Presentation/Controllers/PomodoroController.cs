using DOJO2.Infrastructure.Services;
using DOJO2.Presentation.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DOJO2.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PomodoroController : ControllerBase
{
    private readonly IPomodoroService _pomodoroService;
    private readonly ILogger<PomodoroController> _logger;

    public PomodoroController(IPomodoroService pomodoroService, ILogger<PomodoroController> logger)
    {
        _pomodoroService = pomodoroService ?? throw new ArgumentNullException(nameof(pomodoroService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet("today")]
    public async Task<IActionResult> GetTodayStats()
    {
        var authError = ValidateUserAuthorization();
        if (authError != null)
        {
            return authError;
        }

        var userId = GetCurrentUserId() ?? 0;
        var result = await _pomodoroService.GetTodayStatsAsync(userId, DateTime.UtcNow);

        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message, errors = result.Errors });
        }

        return Ok(new { success = true, message = result.Message, data = result.Data });
    }

    [HttpPost("session")]
    public async Task<IActionResult> SaveSession([FromBody] PomodoroSessionCreateViewModel? model)
    {
        if (model == null)
        {
            return BadRequest(new { success = false, message = "Модель Pomodoro не може бути порожньою" });
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(value => value.Errors).Select(error => error.ErrorMessage).ToList();
            return BadRequest(new { success = false, message = "Невалідні дані", errors });
        }

        var authError = ValidateUserAuthorization();
        if (authError != null)
        {
            return authError;
        }

        var userId = GetCurrentUserId() ?? 0;
        var result = await _pomodoroService.CreateSessionAsync(userId, model);

        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message, errors = result.Errors });
        }

        return Ok(new { success = true, message = "Pomodoro сесію збережено", data = result.Data });
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId)
            ? userId
            : null;
    }

    private IActionResult? ValidateUserAuthorization()
    {
        var userId = GetCurrentUserId();
        if (userId == null || userId <= 0)
        {
            _logger.LogWarning("Невалідний userId або користувач не авторизований для Pomodoro API");
            return Unauthorized(new { success = false, message = "Користувача не знайдено" });
        }

        return null;
    }
}
