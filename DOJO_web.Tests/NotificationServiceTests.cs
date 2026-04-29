using DOJO2.Application.ViewModels;
using DOJO2.Domain.Entities;
using DOJO2.Infrastructure.Data;
using DOJO2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace DOJO_web.Tests;

public class NotificationServiceTests
{
    private const int TestUserId = 1;

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static NotificationService CreateService(AppDbContext context)
    {
        var logger = Mock.Of<ILogger<NotificationService>>();
        return new NotificationService(context, logger);
    }

    private static AppUser CreateUser(int id, int streak = 0, DateOnly? lastCompletionDate = null)
    {
        return new AppUser
        {
            Id = id,
            UserName = $"user{id}",
            Email = $"u{id}@test.com",
            NormalizedUserName = $"USER{id}",
            NormalizedEmail = $"U{ id }@TEST.COM",
            ExpPoints = 0,
            Level = 1,
            CurrentStreak = streak,
            LastCompletionDate = lastCompletionDate,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static TaskItem CreateTask(
        int userId,
        string title,
        DateOnly? dueDate = null,
        DateTime? scheduledAt = null,
        bool isPlan = false,
        bool isCompleted = false)
    {
        return new TaskItem
        {
            UserId = userId,
            Title = title,
            DueDate = dueDate,
            ScheduledAt = scheduledAt,
            IsPlan = isPlan,
            IsCompleted = isCompleted,
            CreatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task GetDashboardNotifications_ReturnsFailure_WhenUserNotFound()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.GetDashboardNotificationsAsync(TestUserId, DateTime.UtcNow);

        Assert.False(result.Success);
        Assert.Contains("Користувача не знайдено", result.Message);
    }

    [Fact]
    public async Task GetDashboardNotifications_ReturnsStreakWarning_WhenUserHasNotCompletedTasksToday()
    {
        using var context = CreateContext();
        var utcNow = new DateTime(2026, 4, 21, 10, 0, 0, DateTimeKind.Utc);
        var today = DateOnly.FromDateTime(utcNow);
        var user = CreateUser(TestUserId, streak: 3, lastCompletionDate: today.AddDays(-1));
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.GetDashboardNotificationsAsync(TestUserId, utcNow);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Contains(result.Data!, n => n.Severity == NotificationSeverity.Warning && n.Title == "Серія під загрозою");
    }

    [Fact]
    public async Task GetDashboardNotifications_ReturnsDeadlineTomorrowNotification_ForIncompletePlan()
    {
        using var context = CreateContext();
        var utcNow = new DateTime(2026, 4, 21, 10, 0, 0, DateTimeKind.Utc);
        var tomorrow = DateOnly.FromDateTime(utcNow).AddDays(1);
        var tomorrowScheduledAt = tomorrow.ToDateTime(new TimeOnly(15, 30), DateTimeKind.Utc);
        var user = CreateUser(TestUserId);
        context.Users.Add(user);
        context.Tasks.Add(CreateTask(TestUserId, "Підготувати звіт", scheduledAt: tomorrowScheduledAt, isPlan: true));
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.GetDashboardNotificationsAsync(TestUserId, utcNow);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Contains(result.Data!, n => n.Severity == NotificationSeverity.Warning && n.Title == "План на завтра");
    }

    [Fact]
    public async Task GetDashboardNotifications_DoesNotReturnDeadlineNotification_ForTodoDueTomorrow()
    {
        using var context = CreateContext();
        var utcNow = new DateTime(2026, 4, 21, 10, 0, 0, DateTimeKind.Utc);
        var tomorrow = DateOnly.FromDateTime(utcNow).AddDays(1);
        var tomorrowStartUtc = tomorrow.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var user = CreateUser(TestUserId);
        context.Users.Add(user);
        context.Tasks.Add(CreateTask(TestUserId, "Звичайне завдання", dueDate: tomorrow, scheduledAt: tomorrowStartUtc, isPlan: false));
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.GetDashboardNotificationsAsync(TestUserId, utcNow);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.DoesNotContain(result.Data!, n => n.Title == "План на завтра");
    }

    [Fact]
    public async Task GetDashboardNotifications_ReturnsInfoNotification_WhenNothingImportantExists()
    {
        using var context = CreateContext();
        var utcNow = new DateTime(2026, 4, 21, 10, 0, 0, DateTimeKind.Utc);
        var user = CreateUser(TestUserId);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.GetDashboardNotificationsAsync(TestUserId, utcNow);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data!);
        Assert.Equal(NotificationSeverity.Info, result.Data![0].Severity);
        Assert.Equal("Наразі все спокійно", result.Data![0].Title);
    }

    [Fact]
    public async Task GetDashboardNotifications_ReturnsFriendRequestNotification()
    {
        using var context = CreateContext();
        var user = CreateUser(TestUserId);
        var requester = CreateUser(2);
        context.Users.AddRange(user, requester);
        context.FriendRequests.Add(new FriendRequest
        {
            RequesterUserId = requester.Id,
            ReceiverUserId = user.Id,
            CreatedAt = new DateTime(2026, 4, 21, 8, 0, 0, DateTimeKind.Utc)
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.GetDashboardNotificationsAsync(TestUserId, DateTime.UtcNow);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Contains(result.Data!, n => n.Title == "Новий запит у друзі" && n.Actions.Count == 2);
    }
}

