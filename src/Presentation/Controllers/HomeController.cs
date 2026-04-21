using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DOJO2.Models;
using DOJO2.Application.Interfaces;
using DOJO2.Application.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using DOJO2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DOJO2.Presentation.Controllers;

public class HomeController : Controller
{
    private readonly IStatisticsService _statisticsService;
    private readonly ILogger<HomeController> _logger;
    private readonly UserManager<AppUser> _userManager;

    public HomeController(IStatisticsService statisticsService, ILogger<HomeController> logger, UserManager<AppUser> userManager)
    {
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
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

    public async Task<IActionResult> GetLeaderboard(int limit = 10)
    {
        try
        {
            var topUsers = await _userManager.Users
                .Include(u => u.Pomodoros)
                .OrderByDescending(u => u.ExpPoints)
                .Take(limit)
                .ToListAsync();

            var leaderboardEntries = topUsers.Select((user, index) => new LeaderboardEntry
            {
                Rank = index + 1,
                Username = user.UserName ?? "Невідомий користувач",
                Score = user.ExpPoints,
                Level = user.Level,
                PomodoroSessions = user.Pomodoros.Count,
                AvatarUrl = string.IsNullOrEmpty(user.AvatarUrl) ? null : user.AvatarUrl
            }).ToList();

            var viewModel = new LeaderboardViewModel { Entries = leaderboardEntries };
            return PartialView("_Leaderboard", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні лідерборду");
            return PartialView("_Leaderboard", new LeaderboardViewModel());
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}