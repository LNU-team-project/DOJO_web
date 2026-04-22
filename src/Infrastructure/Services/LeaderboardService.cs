using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DOJO2.Application.Interfaces;
using DOJO2.Application.ViewModels;
using DOJO2.Domain.Entities;

namespace DOJO2.Application.Services;

public class LeaderboardService : ILeaderboardService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<LeaderboardService> _logger;

    public LeaderboardService(UserManager<AppUser> userManager, ILogger<LeaderboardService> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<LeaderboardViewModel> GetLeaderboardAsync(int limit = 10)
    {
        try
        {
            var topUsers = await _userManager.Users
                .Include(u => u.Pomodoros)
                .OrderByDescending(u => u.ExpPoints)
                .Take(limit)
                .ToListAsync();

            return MapToViewModel(topUsers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні лідерборду");
            return new LeaderboardViewModel { Entries = new List<LeaderboardEntry>() };
        }
    }

    public async Task<LeaderboardViewModel> GetLeaderboardBySortAsync(string sortBy = "xp", int limit = 10)
    {
        try
        {
            IQueryable<AppUser> query = _userManager.Users.Include(u => u.Pomodoros);

            query = sortBy?.ToLower() switch
            {
                "pomodoro" => query.OrderByDescending(u => u.Pomodoros.Count),
                "level" => query.OrderByDescending(u => u.Level).ThenByDescending(u => u.ExpPoints),
                _ => query.OrderByDescending(u => u.ExpPoints) // За замовчуванням - XP
            };

            var users = await query.Take(limit).ToListAsync();
            return MapToViewModel(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при сортуванні лідерборду");
            return new LeaderboardViewModel { Entries = new List<LeaderboardEntry>() };
        }
    }

    public async Task<LeaderboardViewModel> SearchLeaderboardAsync(string searchTerm, int limit = 50)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetLeaderboardAsync(limit);
            }

            var searchLower = searchTerm.ToLower().Trim();
            var users = await _userManager.Users
                .Include(u => u.Pomodoros)
                .Where(u => u.UserName != null && u.UserName.ToLower().Contains(searchLower))
                .OrderByDescending(u => u.ExpPoints)
                .Take(limit)
                .ToListAsync();

            return MapToViewModel(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при пошуку в лідербордоу");
            return new LeaderboardViewModel { Entries = new List<LeaderboardEntry>() };
        }
    }

    public async Task<LeaderboardViewModel> GetFilteredAndSortedLeaderboardAsync(string searchTerm, string sortBy, int limit = 50)
    {
        try
        {
            IQueryable<AppUser> query = _userManager.Users.Include(u => u.Pomodoros);

            // Фільтруємо за пошуком
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchLower = searchTerm.ToLower().Trim();
                query = query.Where(u => u.UserName != null && u.UserName.ToLower().Contains(searchLower));
            }

            // Сортуємо
            query = sortBy?.ToLower() switch
            {
                "pomodoro" => query.OrderByDescending(u => u.Pomodoros.Count),
                "level" => query.OrderByDescending(u => u.Level).ThenByDescending(u => u.ExpPoints),
                _ => query.OrderByDescending(u => u.ExpPoints)
            };

            var users = await query.Take(limit).ToListAsync();
            return MapToViewModel(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при фільтруванні та сортуванні лідерборду");
            return new LeaderboardViewModel { Entries = new List<LeaderboardEntry>() };
        }
    }

    private LeaderboardViewModel MapToViewModel(List<AppUser> users)
    {
        var entries = users.Select((user, index) => new LeaderboardEntry
        {
            Rank = index + 1,
            Username = user.UserName ?? "Невідомий користувач",
            Score = user.ExpPoints,
            Level = user.Level,
            PomodoroSessions = user.Pomodoros?.Count ?? 0,
            AvatarUrl = string.IsNullOrEmpty(user.AvatarUrl) ? null : user.AvatarUrl
        }).ToList();

        return new LeaderboardViewModel { Entries = entries };
    }
}