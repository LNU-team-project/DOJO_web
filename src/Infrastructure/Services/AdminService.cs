using DOJO2.Infrastructure.Data;
using DOJO2.Infrastructure.Results;
using DOJO2.Presentation.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DOJO2.Infrastructure.Services;

public interface IAdminService
{
    Task<Result<bool>> AuthenticateAdminAsync(string login, string password);
    Task<Result<List<AdminUserListItemViewModel>>> GetUsersAsync(string? search);
}

public class AdminService : IAdminService
{
    private const int MaxUsersForAdminPage = 200;
    private readonly AppDbContext _context;
    private readonly ILogger<AdminService> _logger;

    public AdminService(AppDbContext context, ILogger<AdminService> logger)
    {
        _context = context;
        _logger = logger;
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
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        _logger.LogInformation("Адміністратор отримав список користувачів. Кількість: {Count}", users.Count);
        return Result<List<AdminUserListItemViewModel>>.SuccessResult(users, "Користувачів успішно завантажено");
    }
}

