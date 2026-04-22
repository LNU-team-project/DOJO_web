using Microsoft.AspNetCore.Mvc;
using DOJO2.Application.Interfaces;

namespace DOJO2.Presentation.Controllers;

public class LeaderboardController : Controller
{
    private readonly ILeaderboardService _leaderboardService;
    private readonly ILogger<LeaderboardController> _logger;

    public LeaderboardController(ILeaderboardService leaderboardService, ILogger<LeaderboardController> logger)
    {
        _leaderboardService = leaderboardService;
        _logger = logger;
    }

    public async Task<IActionResult> GetLeaderboard(int limit = 10)
    {
        try
        {
            var viewModel = await _leaderboardService.GetLeaderboardAsync(limit);
            return PartialView("_LeaderboardList", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні лідерборду");
            return PartialView("_LeaderboardList", new Application.ViewModels.LeaderboardViewModel());
        }
    }

    public async Task<IActionResult> GetLeaderboardBySort(string sortBy = "xp", int limit = 10)
    {
        try
        {
            var viewModel = await _leaderboardService.GetLeaderboardBySortAsync(sortBy, limit);
            return PartialView("_LeaderboardList", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні відсортованого лідерборду");
            return PartialView("_LeaderboardList", new Application.ViewModels.LeaderboardViewModel());
        }
    }

    public async Task<IActionResult> SearchLeaderboard(string searchTerm, int limit = 50)
    {
        try
        {
            var viewModel = await _leaderboardService.SearchLeaderboardAsync(searchTerm, limit);
            return PartialView("_LeaderboardList", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при пошуку в лідербордоу");
            return PartialView("_LeaderboardList", new Application.ViewModels.LeaderboardViewModel());
        }
    }

    public async Task<IActionResult> GetFilteredAndSorted(string searchTerm, string sortBy = "xp", int limit = 50)
    {
        try
        {
            var viewModel = await _leaderboardService.GetFilteredAndSortedLeaderboardAsync(searchTerm, sortBy, limit);
            return PartialView("_LeaderboardList", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при фільтруванні та сортуванні лідерборду");
            return PartialView("_LeaderboardList", new Application.ViewModels.LeaderboardViewModel());
        }
    }
}