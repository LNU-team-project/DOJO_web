using DOJO2.Application.Interfaces;
using DOJO2.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DOJO2.Controllers;

[Authorize]
public class ScheduleController : BaseApiController
{
    private readonly IScheduleService _scheduleService;

    public ScheduleController(IScheduleService scheduleService, ILogger<ScheduleController> logger)
        : base(logger)
    {
        _scheduleService = scheduleService ?? throw new ArgumentNullException(nameof(scheduleService));
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateSchedule([FromBody] ScheduleCreateViewModel? model)
    {
        var authError = ValidateUserAuthorization();
        if (authError != null)
        {
            return authError;
        }

        var userId = GetCurrentUserId() ?? 0;
        var result = await _scheduleService.CreateScheduleAsync(userId, model);
        return ToActionResult(result);
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetSchedules([FromQuery] DateTime? weekStart, [FromQuery] DateTime? weekEnd)
    {
        var authError = ValidateUserAuthorization();
        if (authError != null)
        {
            return authError;
        }

        var userId = GetCurrentUserId() ?? 0;
        var result = await _scheduleService.GetSchedulesForRangeAsync(userId, weekStart, weekEnd);
        return ToActionResult(result);
    }

    [HttpPost("delete")]
    public async Task<IActionResult> DeleteOccurrence([FromBody] ScheduleDeleteViewModel? model)
    {
        var authError = ValidateUserAuthorization();
        if (authError != null)
        {
            return authError;
        }

        var userId = GetCurrentUserId() ?? 0;
        var result = await _scheduleService.DeleteScheduleOccurrenceAsync(userId, model);
        return ToActionResult(result);
    }
}
