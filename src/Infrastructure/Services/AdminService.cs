using DOJO2.Infrastructure.Data;
using DOJO2.Application.Common;
using DOJO2.Domain.Entities;
using DOJO2.Application.Interfaces;
using DOJO2.Application.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace DOJO2.Infrastructure.Services;

public class AdminService : IAdminService
{
    private const string AdminUsersCacheKeyPrefix = "admin-users";
    private const string AdminUsersCacheVersionKey = "admin-users-cache-version";

    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<AdminService> _logger;
    private readonly AdminUsersOptions _adminUsersOptions;
    private readonly IMemoryCache _cache;
    private readonly CacheOptions _cacheOptions;

    public AdminService(
        AppDbContext context,
        UserManager<AppUser> userManager,
        ILogger<AdminService> logger,
        IOptions<AdminUsersOptions> adminUsersOptions,
        IMemoryCache cache,
        IOptions<CacheOptions> cacheOptions)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _adminUsersOptions = (adminUsersOptions ?? throw new ArgumentNullException(nameof(adminUsersOptions))).Value;
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _cacheOptions = (cacheOptions ?? throw new ArgumentNullException(nameof(cacheOptions))).Value;
    }

    public async Task<Result<bool>> AuthenticateAdminAsync(string login, string password)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("Спроба входу з порожніми параметрами");
            return Result<bool>.FailureResult("Логін та пароль не можуть бути порожніми");
        }

        var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Login == login);
        
        if (admin == null)
        {
            _logger.LogWarning("Адміністратор не знайдено: {AdminLogin}", login);
            return Result<bool>.FailureResult("Неправильний логін або пароль");
        }

        var passwordHasher = new PasswordHasher<string>();
        var verificationResult = passwordHasher.VerifyHashedPassword(string.Empty, admin.Password ?? string.Empty, password);

        if (verificationResult == PasswordVerificationResult.Success)
        {
            _logger.LogInformation("Адміністратор успішно увійшов: {AdminLogin}", login);
            return Result<bool>.SuccessResult(true, "Успішний вхід");
        }

        _logger.LogWarning("Неправильний пароль для адміністратора: {AdminLogin}", login);
        return Result<bool>.FailureResult("Неправильний логін або пароль");
    }

    public async Task<Result<List<AdminUserListItemViewModel>>> GetUsersAsync(string? search)
    {
        var normalizedSearch = search?.Trim() ?? string.Empty;
        var nowUtc = DateTimeOffset.UtcNow;
        var minSearchLength = _adminUsersOptions.MinSearchLength >= 0 ? _adminUsersOptions.MinSearchLength : 0;
        var maxUsersForAdminPage = _adminUsersOptions.MaxUsersForAdminPage > 0 ? _adminUsersOptions.MaxUsersForAdminPage : 200;
        var cacheVersion = GetOrCreateAdminUsersCacheVersion();
        var cacheKey = BuildAdminUsersCacheKey(normalizedSearch, maxUsersForAdminPage, minSearchLength, cacheVersion);

        if (_cache.TryGetValue(cacheKey, out List<AdminUserListItemViewModel>? cachedUsers) && cachedUsers is not null)
        {
            _logger.LogInformation("Повернуто кешований список користувачів для адмін-сторінки. Кількість: {Count}", cachedUsers.Count);
            return Result<List<AdminUserListItemViewModel>>.SuccessResult(cachedUsers, "Користувачів успішно завантажено (cache)");
        }

        var query = _context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(normalizedSearch) && normalizedSearch.Length >= minSearchLength)
        {
            query = query.Where(u =>
                (u.UserName != null && EF.Functions.ILike(u.UserName, $"%{normalizedSearch}%")) ||
                (u.Email != null && EF.Functions.ILike(u.Email, $"%{normalizedSearch}%")));
        }

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Take(maxUsersForAdminPage)
            .Select(u => new AdminUserListItemViewModel
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                Email = u.Email ?? string.Empty,
                Level = u.Level,
                ExpPoints = u.ExpPoints,
                CreatedAt = u.CreatedAt,
                IsBlocked = u.LockoutEnabled && u.LockoutEnd.HasValue && u.LockoutEnd > nowUtc,
                LockoutEnd = u.LockoutEnd
            })
            .ToListAsync();

        var cacheSeconds = _cacheOptions.AdminUsersSeconds > 0 ? _cacheOptions.AdminUsersSeconds : 180;
        _cache.Set(cacheKey, users, TimeSpan.FromSeconds(cacheSeconds));

        _logger.LogInformation("Адміністратор отримав список користувачів. Кількість: {Count}", users.Count);
        return Result<List<AdminUserListItemViewModel>>.SuccessResult(users, "Користувачів успішно завантажено");
    }

    public async Task<Result<bool>> BlockUserAsync(int userId)
    {
        if (userId <= 0)
        {
            return Result<bool>.FailureResult("Невалідний ідентифікатор користувача");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            _logger.LogWarning("Користувач для блокування не знайдено. UserId: {UserId}", userId);
            return Result<bool>.FailureResult("Користувача не знайдено");
        }

        user.LockoutEnabled = true;
        var lockoutYears = _adminUsersOptions.LockoutYears > 0 ? _adminUsersOptions.LockoutYears : 100;
        user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(lockoutYears);

        await _context.SaveChangesAsync();
        InvalidateAdminUsersCache();
        _logger.LogInformation("Користувача заблоковано. UserId: {UserId}", userId);

        return Result<bool>.SuccessResult(true, "Користувача успішно заблоковано");
    }

    public async Task<Result<bool>> UnblockUserAsync(int userId)
    {
        if (userId <= 0)
        {
            return Result<bool>.FailureResult("Невалідний ідентифікатор користувача");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            _logger.LogWarning("Користувач для розблокування не знайдено. UserId: {UserId}", userId);
            return Result<bool>.FailureResult("Користувача не знайдено");
        }

        user.LockoutEnabled = false;
        user.LockoutEnd = null;

        await _context.SaveChangesAsync();
        InvalidateAdminUsersCache();
        _logger.LogInformation("Користувача розблоковано. UserId: {UserId}", userId);

        return Result<bool>.SuccessResult(true, "Користувача успішно розблоковано");
    }

    public async Task<Result<bool>> DeleteUserAsync(int userId)
    {
        if (userId <= 0)
        {
            return Result<bool>.FailureResult("Невалідний ідентифікатор користувача");
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            _logger.LogWarning("Користувач для видалення не знайдено. UserId: {UserId}", userId);
            return Result<bool>.FailureResult("Користувача не знайдено");
        }

        var deleteResult = await _userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
        {
            var errors = deleteResult.Errors.Select(e => e.Description).ToList();
            _logger.LogWarning("Помилка при видаленні користувача {UserId}: {Errors}", userId, string.Join(", ", errors));
            return Result<bool>.FailureResult("Не вдалося видалити користувача", errors);
        }

        InvalidateAdminUsersCache();
        _logger.LogInformation("Адміністратор видалив користувача. UserId: {UserId}", userId);
        return Result<bool>.SuccessResult(true, "Користувача успішно видалено");
    }

    private string GetOrCreateAdminUsersCacheVersion()
    {
        if (_cache.TryGetValue(AdminUsersCacheVersionKey, out string? version) && !string.IsNullOrWhiteSpace(version))
        {
            return version;
        }

        var newVersion = Guid.NewGuid().ToString("N");
        _cache.Set(AdminUsersCacheVersionKey, newVersion);
        return newVersion;
    }

    private void InvalidateAdminUsersCache()
    {
        var newVersion = Guid.NewGuid().ToString("N");
        _cache.Set(AdminUsersCacheVersionKey, newVersion);
    }

    private static string BuildAdminUsersCacheKey(string normalizedSearch, int maxUsersForAdminPage, int minSearchLength, string version)
    {
        return $"{AdminUsersCacheKeyPrefix}:{version}:{normalizedSearch}:{maxUsersForAdminPage}:{minSearchLength}";
    }
}

