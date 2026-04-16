using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DOJO2.Infrastructure.Services;

namespace DOJO2.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;
    private readonly ILogger<StatisticsController> _logger;

    public StatisticsController(IStatisticsService statisticsService, ILogger<StatisticsController> logger)
    {
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet("detailed")]
    public async Task<IActionResult> GetDetailedStatistics([FromQuery] DateTime? startDate = null)
    {
        try
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Не вдалося визначити userId у Claims");
                return BadRequest(new { success = false, message = "Помилка авторизації" });
            }

            var result = await _statisticsService.GetDetailedStatisticsAsync(userId, startDate);
            
            if (!result.Success)
            {
                _logger.LogError("Помилка при отриманні детальної статистики: {Message}", result.Message);
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, data = result.Data, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Непередбачена помилка при отриманні детальної статистики");
            return StatusCode(500, new { success = false, message = "Внутрішня помилка сервера" });
        }
    }

    [HttpGet("today")]
    public async Task<IActionResult> GetTodayStatistics()
    {
        try
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Не вдалося визначити userId у Claims");
                return BadRequest(new { success = false, message = "Помилка авторизації" });
            }

            var result = await _statisticsService.GetTodayStatisticsAsync(userId, DateTime.UtcNow);
            
            if (!result.Success)
            {
                _logger.LogError("Помилка при отриманні статистики за день: {Message}", result.Message);
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, data = result.Data, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Непередбачена помилка при отриманні статистики за день");
            return StatusCode(500, new { success = false, message = "Внутрішня помилка сервера" });
        }
    }

    [HttpGet("weekly")]
    public async Task<IActionResult> GetWeeklyProgress([FromQuery] DateTime? dateInWeek = null)
    {
        try
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Не вдалося визначити userId у Claims");
                return BadRequest(new { success = false, message = "Помилка авторизації" });
            }

            var result = await _statisticsService.GetWeeklyProgressAsync(userId, dateInWeek);
            
            if (!result.Success)
            {
                _logger.LogError("Помилка при отриманні тижневої статистики: {Message}", result.Message);
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new { success = true, data = result.Data, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Непередбачена помилка при отриманні тижневої статистики");
            return StatusCode(500, new { success = false, message = "Внутрішня помилка сервера" });
        }
    }
}
