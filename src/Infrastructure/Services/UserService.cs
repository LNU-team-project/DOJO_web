using DOJO2.Domain.Entities;
using DOJO2.Infrastructure.Results;
using DOJO2.Presentation.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DOJO2.Infrastructure.Services;

public interface IUserService
{
    Task<Result<UserProfileViewModel>> GetUserProfileAsync(int userId);
    Task<Result<UserProfileViewModel>> UpdateUserProfileAsync(int userId, UpdateUserProfileViewModel model);
    Task<Result<bool>> UpdateUserAvatarAsync(int userId, IFormFile avatarFile);
}

public class UserService : IUserService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<UserService> _logger;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private const string AvatarDirectory = "uploads/avatars";
    private const long MaxAvatarSize = 5 * 1024 * 1024; // 5MB
    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    public UserService(
        UserManager<AppUser> userManager,
        ILogger<UserService> logger,
        IWebHostEnvironment webHostEnvironment)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
    }

    public async Task<Result<UserProfileViewModel>> GetUserProfileAsync(int userId)
    {
        if (userId <= 0)
        {
            _logger.LogWarning("Спроба отримати профіль з невалідним userId: {UserId}", userId);
            return Result<UserProfileViewModel>.FailureResult("Невалідний ідентифікатор користувача");
        }

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            _logger.LogWarning("Користувач {UserId} не знайдено", userId);
            return Result<UserProfileViewModel>.FailureResult("Користувача не знайдено");
        }

        var profileVm = MapToProfileViewModel(user);
        _logger.LogInformation("Профіль користувача {UserId} успішно отримано", userId);

        return Result<UserProfileViewModel>.SuccessResult(profileVm, "Профіль успішно отримано");
    }

    public async Task<Result<UserProfileViewModel>> UpdateUserProfileAsync(int userId, UpdateUserProfileViewModel? model)
    {
        if (model == null)
        {
            _logger.LogWarning("Спроба оновити профіль з null моделлю для користувача {UserId}", userId);
            return Result<UserProfileViewModel>.FailureResult("Модель профіля не може бути порожною");
        }

        if (userId <= 0)
        {
            _logger.LogWarning("Спроба оновити профіль з невалідним userId: {UserId}", userId);
            return Result<UserProfileViewModel>.FailureResult("Невалідний ідентифікатор користувача");
        }

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            _logger.LogWarning("Користувач {UserId} не знайдено при оновленні профіля", userId);
            return Result<UserProfileViewModel>.FailureResult("Користувача не знайдено");
        }

        if (!string.IsNullOrWhiteSpace(model.UserName) && model.UserName != user.UserName)
        {
            var userNameExists = await _userManager.Users.AnyAsync(u => u.UserName == model.UserName.Trim());
            if (userNameExists)
            {
                _logger.LogWarning("Ім'я користувача {UserName} вже займає інший користувач", model.UserName);
                return Result<UserProfileViewModel>.FailureResult("Це ім'я користувача вже зареєстровано");
            }

            user.UserName = model.UserName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(model.PhoneNumber))
        {
            user.PhoneNumber = model.PhoneNumber.Trim();
        }

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            _logger.LogWarning("Помилка при оновленні профіля користувача {UserId}: {Errors}", userId, string.Join(", ", errors));
            return Result<UserProfileViewModel>.FailureResult("Не вдалося оновити профіль", errors);
        }

        _logger.LogInformation("Профіль користувача {UserId} успішно оновлено", userId);
        var updatedProfile = MapToProfileViewModel(user);

        return Result<UserProfileViewModel>.SuccessResult(updatedProfile, "Профіль успішно оновлено");
    }

    public async Task<Result<bool>> UpdateUserAvatarAsync(int userId, IFormFile? avatarFile)
    {
        if (userId <= 0)
        {
            _logger.LogWarning("Спроба оновити аватар з невалідним userId: {UserId}", userId);
            return Result<bool>.FailureResult("Невалідний ідентифікатор користувача");
        }

        if (avatarFile == null || avatarFile.Length == 0)
        {
            _logger.LogWarning("Спроба завантажити порожний файл аватара для користувача {UserId}", userId);
            return Result<bool>.FailureResult("Виберіть файл аватара");
        }

        if (avatarFile.Length > MaxAvatarSize)
        {
            _logger.LogWarning("Файл аватара {UserId} перевищує максимальний розмір", userId);
            return Result<bool>.FailureResult("Розмір файлу не може перевищувати 5MB");
        }

        var fileExtension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(fileExtension))
        {
            _logger.LogWarning("Невалідний формат файлу аватара для користувача {UserId}: {Extension}", userId, fileExtension);
            return Result<bool>.FailureResult("Дозволені тільки jpg, jpeg, png та webp формати");
        }

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            _logger.LogWarning("Користувач {UserId} не знайдено при завантаженні аватара", userId);
            return Result<bool>.FailureResult("Користувача не знайдено");
        }

        var uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, AvatarDirectory);
        if (!Directory.Exists(uploadDir))
        {
            Directory.CreateDirectory(uploadDir);
        }

        TryDeleteExistingAvatar(user, userId);

        var fileName = $"{userId}_{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(uploadDir, fileName);
        var avatarUrl = $"/{AvatarDirectory}/{fileName}";

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await avatarFile.CopyToAsync(stream);
        }

        user.AvatarUrl = avatarUrl;
        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            var errors = updateResult.Errors.Select(e => e.Description).ToList();
            _logger.LogWarning("Помилка при оновленні AvatarUrl користувача {UserId}: {Errors}", userId, string.Join(", ", errors));
            
            // Видалити завантажений файл якщо не вдалось оновити БД
            TryDeleteUploadedAvatar(filePath);

            return Result<bool>.FailureResult("Не вдалося зберегти аватар", errors);
        }

        _logger.LogInformation("Аватар користувача {UserId} успішно завантажено: {FileName}", userId, fileName);

        return Result<bool>.SuccessResult(true, "Аватар успішно завантажено");
    }

    private static UserProfileViewModel MapToProfileViewModel(AppUser user)
    {
        return new UserProfileViewModel
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            ExpPoints = user.ExpPoints,
            Level = user.Level,
            CurrentStreak = user.CurrentStreak,
            CreatedAt = user.CreatedAt,
            EmailConfirmed = user.EmailConfirmed,
            AvatarUrl = user.AvatarUrl
        };
    }

    private void TryDeleteExistingAvatar(AppUser user, int userId)
    {
        if (string.IsNullOrWhiteSpace(user.AvatarUrl))
        {
            return;
        }

        var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, user.AvatarUrl.TrimStart('/'));
        if (!File.Exists(oldFilePath))
        {
            return;
        }

        try
        {
            File.Delete(oldFilePath);
            _logger.LogInformation("Старий аватар користувача {UserId} видалено", userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не вдалося видалити старий аватар користувача {UserId}", userId);
        }
    }

    private void TryDeleteUploadedAvatar(string filePath)
    {
        try
        {
            File.Delete(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не вдалося видалити завантажений аватар");
        }
    }
}
