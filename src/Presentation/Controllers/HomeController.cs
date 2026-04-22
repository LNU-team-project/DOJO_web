using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DOJO2.Models;
using DOJO2.Application.Interfaces;
using DOJO2.Application.ViewModels;
using System.Security.Claims;

namespace DOJO2.Presentation.Controllers;

public class HomeController : Controller
{
    private readonly IStatisticsService _statisticsService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IStatisticsService statisticsService, ILogger<HomeController> logger)
    {
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public async Task<IActionResult> Dashboard()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Не вдалося розпарсити userId з Claims");
            return View();
        }

        var result = await _statisticsService.GetTodayStatisticsAsync(userId, DateTime.UtcNow);
        if (result.Success)
        {
            ViewData["Statistics"] = result.Data;
        }
        else
        {
            _logger.LogWarning("Помилка при отриманні статистики: {Message}", result.Message);
        }

        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}