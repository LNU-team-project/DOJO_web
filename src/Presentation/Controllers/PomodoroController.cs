using DOJO2.Application.Interfaces;
using DOJO2.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DOJO2.Controllers;

[Authorize]
public class PomodoroController : BaseApiController
{
    private readonly IPomodoroService _pomodoroService;

    public PomodoroController(IPomodoroService pomodoroService, ILogger<PomodoroController> logger)
        : base(logger)
    {
        _pomodoroService = pomodoroService ?? throw new ArgumentNullException(nameof(pomodoroService));
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

        return ToActionResult(result);
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

        return ToActionResult(result);
    }

    [HttpGet("presets")]
    public async Task<IActionResult> GetPresets()
    {
        var authError = ValidateUserAuthorization();
        if (authError != null)
        {
            return authError;
        }

        var userId = GetCurrentUserId() ?? 0;
        var result = await _pomodoroService.GetPresetsAsync(userId);
        return ToActionResult(result);
    }

    [HttpPost("presets")]
    public async Task<IActionResult> CreatePreset([FromBody] PomodoroPresetCreateViewModel? model)
    {
        if (model == null)
        {
            return BadRequest(new { success = false, message = "Модель пресету не може бути порожньою" });
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
        var result = await _pomodoroService.CreatePresetAsync(userId, model);
        return ToActionResult(result);
    }

    [HttpPut("presets/{presetId}")]
    public async Task<IActionResult> UpdatePreset(int presetId, [FromBody] PomodoroPresetCreateViewModel? model)
    {
        if (model == null)
        {
            return BadRequest(new { success = false, message = "Модель пресету не може бути порожньою" });
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
        var result = await _pomodoroService.UpdatePresetAsync(userId, presetId, model);
        return ToActionResult(result);
    }

    [HttpDelete("presets/{presetId}")]
    public async Task<IActionResult> DeletePreset(int presetId)
    {
        var authError = ValidateUserAuthorization();
        if (authError != null)
        {
            return authError;
        }

        var userId = GetCurrentUserId() ?? 0;
        var result = await _pomodoroService.DeletePresetAsync(userId, presetId);
        return ToActionResult(result);
    }
}
