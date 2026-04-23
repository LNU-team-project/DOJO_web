using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using DOJO2.Application.Common;
using DOJO2.Application.Interfaces;
using DOJO2.Application.ViewModels;
using DOJO2.Domain.Entities;

namespace DOJO2.Application.Services;

public class LeaderboardService : ILeaderboardService
{
    private const string DefaultSortKey = "xp";
    private const int DefaultLeaderboardLimit = 10;
    private const int DefaultSearchLimit = 50;

    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<LeaderboardService> _logger;
    private readonly IMemoryCache _cache;
    private readonly CacheOptions _cacheOptions;

    public LeaderboardService(
        UserManager<AppUser> userManager,
        ILogger<LeaderboardService> logger,
        IMemoryCache cache,
        IOptions<CacheOptions> cacheOptions)
    {
        _userManager = userManager;
        _logger = logger;
        _cache = cache;
        _cacheOptions = cacheOptions.Value;
    }

    public async Task<LeaderboardViewModel> GetLeaderboardAsync(int limit = 10)
    {
        try
        {
            var normalizedLimit = NormalizeLimit(limit, DefaultLeaderboardLimit);
            var cacheKey = BuildCacheKey("leaderboard", DefaultSortKey, string.Empty, normalizedLimit);

            return await GetOrCreateCachedLeaderboardAsync(cacheKey, async () =>
            {
                var topUsers = await _userManager.Users
                    .Include(u => u.Pomodoros)
                    .OrderByDescending(u => u.ExpPoints)
                    .Take(normalizedLimit)
                    .ToListAsync();

                return MapToViewModel(topUsers);
            });
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
            var normalizedSort = NormalizeSortBy(sortBy);
            var normalizedLimit = NormalizeLimit(limit, DefaultLeaderboardLimit);
            var cacheKey = BuildCacheKey("leaderboard", normalizedSort, string.Empty, normalizedLimit);

            return await GetOrCreateCachedLeaderboardAsync(cacheKey, async () =>
            {
                IQueryable<AppUser> query = _userManager.Users.Include(u => u.Pomodoros);

                query = normalizedSort switch
                {
                    "pomodoro" => query.OrderByDescending(u => u.Pomodoros.Count),
                    "level" => query.OrderByDescending(u => u.Level).ThenByDescending(u => u.ExpPoints),
                    _ => query.OrderByDescending(u => u.ExpPoints)
                };

                var users = await query.Take(normalizedLimit).ToListAsync();
                return MapToViewModel(users);
            });
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
            var normalizedLimit = NormalizeLimit(limit, DefaultSearchLimit);
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetLeaderboardAsync(normalizedLimit);
            }

            var normalizedSearch = NormalizeSearchTerm(searchTerm);
            var cacheKey = BuildCacheKey("leaderboard-search", DefaultSortKey, normalizedSearch, normalizedLimit);

            return await GetOrCreateCachedLeaderboardAsync(cacheKey, async () =>
            {
                var users = await _userManager.Users
                    .Include(u => u.Pomodoros)
                    .Where(u => u.UserName != null && u.UserName.ToLower().Contains(normalizedSearch))
                    .OrderByDescending(u => u.ExpPoints)
                    .Take(normalizedLimit)
                    .ToListAsync();

                return MapToViewModel(users);
            });
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
            var normalizedSort = NormalizeSortBy(sortBy);
            var normalizedSearch = NormalizeSearchTerm(searchTerm);
            var normalizedLimit = NormalizeLimit(limit, DefaultSearchLimit);
            var cacheKey = BuildCacheKey("leaderboard-filtered", normalizedSort, normalizedSearch, normalizedLimit);

            return await GetOrCreateCachedLeaderboardAsync(cacheKey, async () =>
            {
                IQueryable<AppUser> query = _userManager.Users.Include(u => u.Pomodoros);

                if (!string.IsNullOrWhiteSpace(normalizedSearch))
                {
                    query = query.Where(u => u.UserName != null && u.UserName.ToLower().Contains(normalizedSearch));
                }

                query = normalizedSort switch
                {
                    "pomodoro" => query.OrderByDescending(u => u.Pomodoros.Count),
                    "level" => query.OrderByDescending(u => u.Level).ThenByDescending(u => u.ExpPoints),
                    _ => query.OrderByDescending(u => u.ExpPoints)
                };

                var users = await query.Take(normalizedLimit).ToListAsync();
                return MapToViewModel(users);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при фільтруванні та сортуванні лідерборду");
            return new LeaderboardViewModel { Entries = new List<LeaderboardEntry>() };
        }
    }

    private async Task<LeaderboardViewModel> GetOrCreateCachedLeaderboardAsync(
        string cacheKey,
        Func<Task<LeaderboardViewModel>> factory)
    {
        if (_cache.TryGetValue(cacheKey, out LeaderboardViewModel? cachedResult) && cachedResult is not null)
        {
            return cachedResult;
        }

        var loadedResult = await factory();
        _cache.Set(
            cacheKey,
            loadedResult,
            TimeSpan.FromSeconds(_cacheOptions.LeaderboardSeconds));

        return loadedResult;
    }

    private static int NormalizeLimit(int limit, int defaultLimit)
    {
        return limit > 0 ? limit : defaultLimit;
    }

    private static string NormalizeSearchTerm(string searchTerm)
    {
        return string.IsNullOrWhiteSpace(searchTerm)
            ? string.Empty
            : searchTerm.Trim().ToLowerInvariant();
    }

    private static string NormalizeSortBy(string sortBy)
    {
        return string.IsNullOrWhiteSpace(sortBy)
            ? DefaultSortKey
            : sortBy.Trim().ToLowerInvariant();
    }

    private static string BuildCacheKey(string prefix, string sortBy, string searchTerm, int limit)
    {
        return $"{prefix}:{sortBy}:{searchTerm}:{limit}";
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