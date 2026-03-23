using DOJO2.Infrastructure.Data;
using DOJO2.Infrastructure.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DOJO2.Infrastructure.Services;

public interface IAdminService
{
    Task<Result<bool>> AuthenticateAdminAsync(string login, string password);
}

public class AdminService : IAdminService
{
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
}

