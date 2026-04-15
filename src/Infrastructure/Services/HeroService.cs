using DOJO2.Domain.Entities;
using DOJO2.Infrastructure.Data;
using DOJO2.Infrastructure.Results;
using DOJO2.Presentation.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace DOJO2.Infrastructure.Services;

public interface IHeroService
{
    Task<Result<HeroStatusViewModel>> GetHeroStatusAsync(int userId);
    Task<Result<HeroStatusViewModel>> AwardExpForTaskAsync(int taskId, int userId);
}

public class HeroService : IHeroService
{
    private const int ExpForPlan = 100;
    private const int ExpForTodo = 50;
    private const int ExpToNextLevel = 300;

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

        // Відмічаємо, що XP за це завдання вже нараховано
        task.XpAwarded = true;
        _context.Tasks.Update(task);

        // Ми поки не реалізуємо перехід на новий рівень (завдання користувача)
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Нараховано {Award} XP користувачу {UserId} за завдання {TaskId}", award, userId, taskId);

        var vm = MapToViewModel(user);
        return Result<HeroStatusViewModel>.SuccessResult(vm, $"Нараховано {award} XP");
    }

    private static HeroStatusViewModel MapToViewModel(AppUser user)
    {
        var expPoints = user.ExpPoints;
        var progressPercent = (int)Math.Round((Math.Min(expPoints, ExpToNextLevel) / (double)ExpToNextLevel) * 100);

        return new HeroStatusViewModel
        {
            Level = user.Level,
            ExpPoints = user.ExpPoints,
            ExpToNextLevel = ExpToNextLevel,
            ProgressPercent = progressPercent
        };
    }
}
