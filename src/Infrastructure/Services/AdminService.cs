using DOJO2.Infrastructure.Data;
using DOJO2.Application.Common;
using DOJO2.Domain.Entities;
using DOJO2.Application.Interfaces;
using DOJO2.Application.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DOJO2.Infrastructure.Services;

public class AdminService : IAdminService
{
    private const int MaxUsersForAdminPage = 200;
    private const int LockoutYears = 100;
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        AppDbContext context,
        UserManager<AppUser> userManager,
        ILogger<AdminService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        var normalizedSearch = search?.Trim();
        var nowUtc = DateTimeOffset.UtcNow;

        var query = _context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(u =>
                (u.UserName != null && EF.Functions.ILike(u.UserName, $"%{normalizedSearch}%")) ||
                (u.Email != null && EF.Functions.ILike(u.Email, $"%{normalizedSearch}%")));
        }

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Take(MaxUsersForAdminPage)
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
        user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(LockoutYears);

        await _context.SaveChangesAsync();
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

        _logger.LogInformation("Адміністратор видалив користувача. UserId: {UserId}", userId);
        return Result<bool>.SuccessResult(true, "Користувача успішно видалено");
    }
}

