using System.Linq;
using DOJO2.Domain.Entities;
using DOJO2.Application.Common;
using DOJO2.Application.Interfaces;
using DOJO2.Application.ViewModels;
using DOJO2.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DOJO2.Infrastructure.Services;

public class UserService : IUserService
{
    private const string InvalidUserIdMessage = "Невалідний ідентифікатор користувача";
    private const string UserNotFoundMessage = "Користувача не знайдено";
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<UserService> _logger;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IAppDbContext _context;
    private const string AvatarDirectory = "uploads/avatars";
    private const long MaxAvatarSize = 5 * 1024 * 1024; // 5MB
    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    public UserService(
        UserManager<AppUser> userManager,
        ILogger<UserService> logger,
        IWebHostEnvironment webHostEnvironment,
        IAppDbContext context)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Result<List<FriendViewModel>>> GetFriendsAsync(int userId)
    {
        if (userId <= 0)
        {
            _logger.LogWarning("GetFriendsAsync called with invalid userId: {UserId}", userId);
            return Result<List<FriendViewModel>>.FailureResult("Невалідний ідентифікатор користувача");
        }

        var friends = await _context.Friends
            .Where(f => f.UserId == userId)
            .Select(f => new FriendViewModel
            {
                Id = f.Id,
                FriendUserId = f.FriendUserId,
                FriendUserName = f.FriendUser != null ? f.FriendUser.UserName ?? string.Empty : string.Empty,
                AvatarUrl = f.FriendUser != null ? f.FriendUser.AvatarUrl : null,
                CreatedAt = f.CreatedAt
            })
            .ToListAsync();

        return Result<List<FriendViewModel>>.SuccessResult(friends, "Друзі успішно завантажені");
    }

    public async Task<Result<bool>> AddFriendAsync(int userId, int friendUserId)
    {
        if (userId <= 0 || friendUserId <= 0)
        {
            return Result<bool>.FailureResult("Невалідний ідентифікатор користувача");
        }

        if (userId == friendUserId)
        {
            return Result<bool>.FailureResult("Неможливо додати себе в друзі");
        }

        var friendUser = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == friendUserId);
        if (friendUser == null)
        {
            return Result<bool>.FailureResult("Користувача для додавання не знайдено");
        }

        var alreadyFriends = await AreFriendsAsync(userId, friendUserId);
        if (alreadyFriends)
        {
            return Result<bool>.FailureResult("Цей користувач уже в списку друзів");
        }

        await AddFriendshipAsync(userId, friendUserId);

        return Result<bool>.SuccessResult(true, "Додано до друзів");
    }

    public async Task<Result<bool>> AddFriendByUserNameAsync(int userId, string friendUserName)
    {
        if (userId <= 0)
        {
            return Result<bool>.FailureResult("Невалідний ідентифікатор користувача");
        }

        if (string.IsNullOrWhiteSpace(friendUserName))
        {
            return Result<bool>.FailureResult("Ім'я користувача не може бути порожнім");
        }

        return await SendFriendRequestAsync(userId, friendUserName);
    }

    public async Task<Result<bool>> RemoveFriendAsync(int userId, int friendUserId)
    {
        if (userId <= 0 || friendUserId <= 0)
        {
            return Result<bool>.FailureResult("Невалідний ідентифікатор користувача");
        }

        var entries = await _context.Friends
            .Where(f => (f.UserId == userId && f.FriendUserId == friendUserId)
                || (f.UserId == friendUserId && f.FriendUserId == userId))
            .ToListAsync();

        if (entries.Count == 0)
        {
            return Result<bool>.FailureResult("Запис не знайдено");
        }

        _context.Friends.RemoveRange(entries);
        await _context.SaveChangesAsync();

        return Result<bool>.SuccessResult(true, "Видалено з друзів");
    }

    public async Task<Result<bool>> SendFriendRequestAsync(int userId, string friendUserName)
    {
        if (userId <= 0)
        {
            return Result<bool>.FailureResult("Невалідний ідентифікатор користувача");
        }

        if (string.IsNullOrWhiteSpace(friendUserName))
        {
            return Result<bool>.FailureResult("Ім'я користувача не може бути порожнім");
        }

        var normalizedName = friendUserName.Trim().ToUpperInvariant();
        var friendUser = await _userManager.Users
            .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedName);

        if (friendUser == null)
        {
            return Result<bool>.FailureResult("Користувача для додавання не знайдено");
        }

        if (friendUser.Id == userId)
        {
            return Result<bool>.FailureResult("Неможливо додати себе в друзі");
        }

        var alreadyFriends = await AreFriendsAsync(userId, friendUser.Id);
        if (alreadyFriends)
        {
            return Result<bool>.FailureResult("Цей користувач уже в списку друзів");
        }

        var incomingRequest = await _context.FriendRequests
            .FirstOrDefaultAsync(fr => fr.RequesterUserId == friendUser.Id && fr.ReceiverUserId == userId);

        if (incomingRequest != null)
        {
            await AcceptFriendRequestInternalAsync(incomingRequest, userId);
            return Result<bool>.SuccessResult(true, "Запит прийнято");
        }

        var existingRequest = await _context.FriendRequests
            .AnyAsync(fr => fr.RequesterUserId == userId && fr.ReceiverUserId == friendUser.Id);

        if (existingRequest)
        {
            return Result<bool>.FailureResult("Запит уже надіслано");
        }

        var request = new FriendRequest
        {
            RequesterUserId = userId,
            ReceiverUserId = friendUser.Id,
            CreatedAt = DateTime.UtcNow
        };

        await _context.FriendRequests.AddAsync(request);
        await _context.SaveChangesAsync();

        return Result<bool>.SuccessResult(true, "Запит у друзі надіслано");
    }

    public async Task<Result<List<FriendRequestViewModel>>> GetIncomingFriendRequestsAsync(int userId)
    {
        if (userId <= 0)
        {
            return Result<List<FriendRequestViewModel>>.FailureResult("Невалідний ідентифікатор користувача");
        }

        var requests = await _context.FriendRequests
            .Where(fr => fr.ReceiverUserId == userId)
            .OrderByDescending(fr => fr.CreatedAt)
            .Select(fr => new FriendRequestViewModel
            {
                RequestId = fr.Id,
                RequesterUserId = fr.RequesterUserId,
                RequesterUserName = fr.RequesterUser != null ? fr.RequesterUser.UserName ?? string.Empty : string.Empty,
                AvatarUrl = fr.RequesterUser != null ? fr.RequesterUser.AvatarUrl : null,
                CreatedAt = fr.CreatedAt
            })
            .ToListAsync();

        return Result<List<FriendRequestViewModel>>.SuccessResult(requests, "Запити завантажено");
    }

    public async Task<Result<bool>> AcceptFriendRequestAsync(int userId, int requestId)
    {
        if (userId <= 0 || requestId <= 0)
        {
            return Result<bool>.FailureResult("Невалідні дані запиту");
        }

        var request = await _context.FriendRequests
            .FirstOrDefaultAsync(fr => fr.Id == requestId && fr.ReceiverUserId == userId);

        if (request == null)
        {
            return Result<bool>.FailureResult("Запит не знайдено");
        }

        await AcceptFriendRequestInternalAsync(request, userId);
        return Result<bool>.SuccessResult(true, "Запит прийнято");
    }

    public async Task<Result<bool>> DeclineFriendRequestAsync(int userId, int requestId)
    {
        if (userId <= 0 || requestId <= 0)
        {
            return Result<bool>.FailureResult("Невалідні дані запиту");
        }

        var request = await _context.FriendRequests
            .FirstOrDefaultAsync(fr => fr.Id == requestId && fr.ReceiverUserId == userId);

        if (request == null)
        {
            return Result<bool>.FailureResult("Запит не знайдено");
        }

        _context.FriendRequests.Remove(request);
        await _context.SaveChangesAsync();

        return Result<bool>.SuccessResult(true, "Запит відхилено");
    }

    public async Task<Result<List<UserSearchViewModel>>> SearchUsersAsync(int userId, string query, int limit)
    {
        if (userId <= 0)
        {
            return Result<List<UserSearchViewModel>>.FailureResult("Невалідний ідентифікатор користувача");
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return Result<List<UserSearchViewModel>>.SuccessResult(new List<UserSearchViewModel>(), "Порожній запит");
        }

        var normalizedQuery = query.Trim().ToUpperInvariant();
        var safeLimit = Math.Clamp(limit, 1, 10);

        var friendIds = _context.Friends
            .Where(f => f.UserId == userId)
            .Select(f => f.FriendUserId);

        var pendingIds = _context.FriendRequests
            .Where(fr => fr.RequesterUserId == userId || fr.ReceiverUserId == userId)
            .Select(fr => fr.RequesterUserId == userId ? fr.ReceiverUserId : fr.RequesterUserId);

        var users = await _userManager.Users
            .Where(u => u.Id != userId
                && u.NormalizedUserName != null
                && u.NormalizedUserName.Contains(normalizedQuery)
                && !friendIds.Contains(u.Id)
                && !pendingIds.Contains(u.Id))
            .OrderBy(u => u.UserName)
            .Select(u => new UserSearchViewModel
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                AvatarUrl = u.AvatarUrl
            })
            .Take(safeLimit)
            .ToListAsync();

        return Result<List<UserSearchViewModel>>.SuccessResult(users, "Пошук завершено");
    }

    private async Task AcceptFriendRequestInternalAsync(FriendRequest request, int userId)
    {
        await AddFriendshipAsync(request.RequesterUserId, request.ReceiverUserId);
        _context.FriendRequests.Remove(request);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Friend request {RequestId} accepted by user {UserId}", request.Id, userId);
    }

    private async Task AddFriendshipAsync(int userId, int friendUserId)
    {
        var now = DateTime.UtcNow;
        var entries = new List<Friend>
        {
            new Friend { UserId = userId, FriendUserId = friendUserId, CreatedAt = now },
            new Friend { UserId = friendUserId, FriendUserId = userId, CreatedAt = now }
        };

        var existing = await _context.Friends
            .Where(f => (f.UserId == userId && f.FriendUserId == friendUserId)
                || (f.UserId == friendUserId && f.FriendUserId == userId))
            .Select(f => new { f.UserId, f.FriendUserId })
            .ToListAsync();

        foreach (var entry in entries)
        {
            if (existing.Any(e => e.UserId == entry.UserId && e.FriendUserId == entry.FriendUserId))
            {
                continue;
            }
            await _context.Friends.AddAsync(entry);
        }

        await _context.SaveChangesAsync();
    }

    private Task<bool> AreFriendsAsync(int userId, int friendUserId)
    {
        return _context.Friends.AnyAsync(f =>
            (f.UserId == userId && f.FriendUserId == friendUserId)
            || (f.UserId == friendUserId && f.FriendUserId == userId));
    }

    public async Task<Result<UserProfileViewModel>> GetUserProfileAsync(int userId)
    {
        if (userId <= 0)
        {
            _logger.LogWarning("Спроба отримати профіль з невалідним userId: {UserId}", userId);
            return Result<UserProfileViewModel>.FailureResult(InvalidUserIdMessage);
        }

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            _logger.LogWarning("Користувач {UserId} не знайдено", userId);
            return Result<UserProfileViewModel>.FailureResult(UserNotFoundMessage);
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
            return Result<UserProfileViewModel>.FailureResult(InvalidUserIdMessage);
        }

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            _logger.LogWarning("Користувач {UserId} не знайдено при оновленні профіля", userId);
            return Result<UserProfileViewModel>.FailureResult(UserNotFoundMessage);
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

    public async Task<Result<bool>> UpdateUserAvatarAsync(int userId, FileUploadData avatarFile)
    {
        if (userId <= 0)
        {
            _logger.LogWarning("Спроба оновити аватар з невалідним userId: {UserId}", userId);
            return Result<bool>.FailureResult(InvalidUserIdMessage);
        }

        if (avatarFile.Length == 0 || avatarFile.Content.Length == 0)
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
            return Result<bool>.FailureResult(UserNotFoundMessage);
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

        await File.WriteAllBytesAsync(filePath, avatarFile.Content);

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

    public async Task<Result<bool>> DeleteUserAccountAsync(int userId)
    {
        if (userId <= 0)
        {
            _logger.LogWarning("Спроба видалити акаунт з невалідним userId: {UserId}", userId);
            return Result<bool>.FailureResult(InvalidUserIdMessage);
        }

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            _logger.LogWarning("Користувач {UserId} не знайдено при видаленні акаунта", userId);
            return Result<bool>.FailureResult(UserNotFoundMessage);
        }

        var avatarUrl = user.AvatarUrl;
        var deleteResult = await _userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
        {
            var errors = deleteResult.Errors.Select(e => e.Description).ToList();
            _logger.LogWarning("Помилка при видаленні акаунта {UserId}: {Errors}", userId, string.Join(", ", errors));
            return Result<bool>.FailureResult("Не вдалося видалити акаунт", errors);
        }

        TryDeleteAvatarFileByUrl(avatarUrl, userId);
        _logger.LogInformation("Акаунт користувача {UserId} успішно видалено", userId);

        return Result<bool>.SuccessResult(true, "Акаунт успішно видалено");
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

    private void TryDeleteAvatarFileByUrl(string? avatarUrl, int userId)
    {
        if (string.IsNullOrWhiteSpace(avatarUrl))
        {
            return;
        }

        var webRootPath = _webHostEnvironment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            return;
        }

        var relativePath = avatarUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var candidatePath = Path.GetFullPath(Path.Combine(webRootPath, relativePath));
        var rootPath = Path.GetFullPath(webRootPath);

        if (!candidatePath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Спроба видалити аватар за межами web root для користувача {UserId}", userId);
            return;
        }

        if (!File.Exists(candidatePath))
        {
            return;
        }

        try
        {
            File.Delete(candidatePath);
            _logger.LogInformation("Файл аватара користувача {UserId} видалено", userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не вдалося видалити файл аватара користувача {UserId}", userId);
        }
    }
}
