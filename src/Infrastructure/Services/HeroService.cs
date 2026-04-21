using DOJO2.Domain.Entities;
using DOJO2.Infrastructure.Data;
using DOJO2.Application.Common;
using DOJO2.Application.Interfaces;
using DOJO2.Application.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace DOJO2.Infrastructure.Services;

public class HeroService : IHeroService
{
    private const int ExpForPlan = 100;
    private const int ExpForTodo = 50;
    private const int ExpToNextLevel = 300;
    private const string StreakSingular = "день підряд";
    private const string StreakFew = "дні підряд";
    private const string StreakMany = "днів підряд";

    private readonly AppDbContext _context;
    private readonly ILogger<HeroService> _logger;

    public HeroService(AppDbContext context, ILogger<HeroService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<HeroStatusViewModel>> GetHeroStatusAsync(int userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            _logger.LogWarning("Користувача {UserId} не знайдено при отриманні статусу героя", userId);
            return Result<HeroStatusViewModel>.FailureResult("Користувача не знайдено");
        }

        await NormalizeStreakForReadAsync(user);
        var vm = MapToViewModel(user);
        return Result<HeroStatusViewModel>.SuccessResult(vm, "Статус героя завантажено");
    }

    public async Task<Result<HeroStatusViewModel>> AwardExpForTaskAsync(int taskId, int userId)
    {
        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
        if (task == null)
        {
            _logger.LogWarning("Завдання {TaskId} не знайдено для користувача {UserId}", taskId, userId);
            return Result<HeroStatusViewModel>.FailureResult("Завдання не знайдено");
        }

        if (task.XpAwarded)
        {
            _logger.LogInformation("XP вже було нараховано за завдання {TaskId} користувачу {UserId}", taskId, userId);
            return Result<HeroStatusViewModel>.FailureResult("XP вже нараховано за це завдання");
        }

        // визначаємо скільки давати XP (плани дорожчі)
        var award = task.IsPlan ? ExpForPlan : ExpForTodo;

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            _logger.LogWarning("Користувача {UserId} не знайдено при нарахуванні XP", userId);
            return Result<HeroStatusViewModel>.FailureResult("Користувача не знайдено");
        }

        user.ExpPoints += award;
        ApplyCompletionStreak(user);

        // Відмічаємо, що XP за це завдання вже нараховано
        task.XpAwarded = true;
        _context.Tasks.Update(task);

        // Обчислюємо новий рівень за кумулятивними очками
        var prevLevel = user.Level;
        var newLevel = (user.ExpPoints / ExpToNextLevel) + 1; // integer division
        var levelsGained = Math.Max(0, newLevel - prevLevel);
        var hasLeveledUp = levelsGained > 0;

        if (hasLeveledUp)
        {
            user.Level = newLevel;
        }

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Нараховано {Award} XP користувачу {UserId} за завдання {TaskId}. Level up: {HasLeveledUp}", award, userId, taskId, hasLeveledUp);

        var vm = MapToViewModel(user);
        vm.HasLeveledUp = hasLeveledUp;
        vm.LevelsGained = levelsGained;

        return Result<HeroStatusViewModel>.SuccessResult(vm, $"Нараховано {award} XP");
    }

    private async Task NormalizeStreakForReadAsync(AppUser user)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var effectiveStreak = GetEffectiveStreak(user, today);

        if (user.CurrentStreak == effectiveStreak)
        {
            return;
        }

        user.CurrentStreak = effectiveStreak;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    private static int GetEffectiveStreak(AppUser user, DateOnly today)
    {
        if (user.LastCompletionDate == null)
        {
            return 0;
        }

        var daysSinceLastCompletion = today.DayNumber - user.LastCompletionDate.Value.DayNumber;
        if (daysSinceLastCompletion < 0)
        {
            return Math.Max(user.CurrentStreak, 0);
        }

        if (daysSinceLastCompletion > 1)
        {
            return 0;
        }

        return Math.Max(user.CurrentStreak, 1);
    }

    private static void ApplyCompletionStreak(AppUser user)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var newStreak = user.LastCompletionDate switch
        {
            null => 1,
            var last when last.Value == today => Math.Max(user.CurrentStreak, 1),
            var last when today.DayNumber - last.Value.DayNumber == 1 => Math.Max(user.CurrentStreak, 0) + 1,
            _ => 1
        };

        user.CurrentStreak = newStreak;
        user.LastCompletionDate = today;
    }

    private static HeroStatusViewModel MapToViewModel(AppUser user)
    {
        var expPoints = user.ExpPoints;
        // Прогрес для поточного рівня = залишок від ділення на поріг
        var remainder = expPoints % ExpToNextLevel;
        var progressPercent = (int)Math.Round((remainder / (double)ExpToNextLevel) * 100);
        var remainingToNext = ExpToNextLevel - remainder;
        var streakText = FormatStreakText(user.CurrentStreak);

        return new HeroStatusViewModel
        {
            Level = user.Level,
            ExpPoints = user.ExpPoints,
            ExpToNextLevel = ExpToNextLevel,
            ProgressPercent = progressPercent,
            CurrentStreak = user.CurrentStreak,
            StreakText = streakText,
            ExpToLevelRemaining = remainingToNext
        };
    }

    private static string FormatStreakText(int streak)
    {
        if (streak <= 0)
        {
            return "Почни серію сьогодні";
        }

        var lastTwoDigits = streak % 100;
        if (lastTwoDigits is >= 11 and <= 14)
        {
            return $"{streak} {StreakMany}";
        }

        return (streak % 10) switch
        {
            1 => $"{streak} {StreakSingular}",
            2 or 3 or 4 => $"{streak} {StreakFew}",
            _ => $"{streak} {StreakMany}"
        };
    }
}
