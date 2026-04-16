using DOJO2.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System;

namespace DOJO2.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CalendarController : ControllerBase
{
    private readonly ICalendarService _calendarService;
    private readonly ILogger<CalendarController> _logger;

    public CalendarController(ICalendarService calendarService, ILogger<CalendarController> logger)
    {
        _calendarService = calendarService ?? throw new ArgumentNullException(nameof(calendarService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            _logger.LogWarning("Невалідний userId або користувач не авторизований");
            return Unauthorized(new { success = false, message = "Користувача не знайдено" });
        }

        return null;
    }

    [HttpGet("marks")]
    public async Task<IActionResult> GetMarks([FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        var authError = ValidateUserAuthorization();
        if (authError != null)
        {
            return authError;
        }

        var userId = GetCurrentUserId() ?? 0;
        var result = await _calendarService.GetMarkedDatesAsync(userId, from, to);

        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message, errors = result.Errors });
        }

        return Ok(new { success = true, message = result.Message, data = result.Data });
    }
}
