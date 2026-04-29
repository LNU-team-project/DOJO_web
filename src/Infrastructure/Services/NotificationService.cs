using DOJO2.Application.Common;
using DOJO2.Application.Interfaces;
using DOJO2.Application.ViewModels;
using DOJO2.Domain.Entities;
using DOJO2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DOJO2.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(AppDbContext context, ILogger<NotificationService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<IReadOnlyList<DashboardNotificationViewModel>>> GetDashboardNotificationsAsync(int userId, DateTime utcNow)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            _logger.LogWarning("Користувача {UserId} не знайдено при отриманні сповіщень", userId);
            return Result<IReadOnlyList<DashboardNotificationViewModel>>.FailureResult("Користувача не знайдено");
        }

        var today = DateOnly.FromDateTime(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));
        var tomorrow = today.AddDays(1);
        var notifications = new List<DashboardNotificationViewModel>();

        AddStreakWarning(user, today, notifications);
        await AddPlanDueTomorrowNotificationsAsync(userId, tomorrow, notifications);
        await AddFriendRequestNotificationsAsync(userId, notifications);

        if (notifications.Count == 0)
        {
            notifications.Add(new DashboardNotificationViewModel
            {
                Severity = NotificationSeverity.Info,
                Badge = "Інфо",
                Title = "Наразі все спокійно",
                Description = "Важливих сповіщень поки немає. Так тримати!"
            });
        }

        var orderedNotifications = notifications
            .OrderByDescending(notification => notification.Severity)
            .ToList();

        _logger.LogInformation("Сповіщення успішно згенеровано для користувача {UserId}", userId);
        IReadOnlyList<DashboardNotificationViewModel> result = orderedNotifications;
        return Result<IReadOnlyList<DashboardNotificationViewModel>>.SuccessResult(result, "Сповіщення успішно згенеровано");
    }

    private async Task AddPlanDueTomorrowNotificationsAsync(
        int userId,
        DateOnly tomorrow,
        ICollection<DashboardNotificationViewModel> notifications)
    {
        var tomorrowStartUtc = tomorrow.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var tasksDueTomorrow = await _context.Tasks.AsNoTracking()
            .Where(task => task.UserId == userId
                && !task.IsCompleted
                && task.IsPlan
                && task.ScheduledAt.HasValue
                && task.ScheduledAt.Value.Date == tomorrowStartUtc.Date)
            .OrderBy(task => task.ScheduledAt)
            .ThenBy(task => task.Title)
            .ToListAsync();

        foreach (var plan in tasksDueTomorrow)
        {
            notifications.Add(new DashboardNotificationViewModel
            {
                Severity = NotificationSeverity.Warning,
                Badge = "Дедлайн",
                Title = "План на завтра",
                Description = $"Завтра спливає дедлайн для плану \"{plan.Title}\"."
            });
        }
    }

    private static void AddStreakWarning(AppUser user, DateOnly today, ICollection<DashboardNotificationViewModel> notifications)
    {
        if (user.CurrentStreak <= 0 || user.LastCompletionDate is null)
        {
            return;
        }

        var daysSinceLastCompletion = today.DayNumber - user.LastCompletionDate.Value.DayNumber;
        if (daysSinceLastCompletion <= 0)
        {
            return;
        }

        var description = daysSinceLastCompletion == 1
            ? "Ви сьогодні ще не виконували завдання. Виконайте хоча б одне, щоб не втратити серію."
            : "Ви давно не виконували завдання. Серія може бути втрачена — поверніться до завдань, щоб відновити ритм.";

        notifications.Add(new DashboardNotificationViewModel
        {
            Severity = NotificationSeverity.Warning,
            Badge = "Серія",
            Title = "Серія під загрозою",
            Description = description
        });
    }

    private async Task AddFriendRequestNotificationsAsync(
        int userId,
        ICollection<DashboardNotificationViewModel> notifications)
    {
        var requests = await _context.FriendRequests.AsNoTracking()
            .Where(fr => fr.ReceiverUserId == userId)
            .OrderByDescending(fr => fr.CreatedAt)
            .Select(fr => new
            {
                fr.Id,
                RequesterName = fr.RequesterUser != null ? fr.RequesterUser.UserName : null
            })
            .ToListAsync();

        foreach (var request in requests)
        {
            var requesterName = request.RequesterName ?? "Користувач";
            notifications.Add(new DashboardNotificationViewModel
            {
                Severity = NotificationSeverity.Warning,
                Badge = "Друзі",
                Title = "Новий запит у друзі",
                Description = $"{requesterName} надіслав запит у друзі.",
                Actions = new List<NotificationActionViewModel>
                {
                    new()
                    {
                        Label = "Прийняти",
                        Action = "accept",
                        RequestId = request.Id
                    },
                    new()
                    {
                        Label = "Відхилити",
                        Action = "decline",
                        RequestId = request.Id
                    }
                }
            });
        }
    }
}


