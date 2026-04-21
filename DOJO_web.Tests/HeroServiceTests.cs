using DOJO2.Infrastructure.Data;
using DOJO2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace DOJO_web.Tests;

public class HeroServiceTests
{
    private const int TestUserId = 1;

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static ILogger<HeroService> CreateMockLogger()
    {
        return Mock.Of<ILogger<HeroService>>();
    }

    private static HeroService CreateService(AppDbContext context)
    {
        var logger = CreateMockLogger();
        return new HeroService(context, logger);
    }

    private static DOJO2.Domain.Entities.TaskItem CreateTaskItem(int userId, string title, bool isPlan = false)
    {
        return new DOJO2.Domain.Entities.TaskItem
        {
            UserId = userId,
            Title = title,
            IsPlan = isPlan,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static DOJO2.Domain.Entities.AppUser CreateUser(
        int id,
        int exp = 0,
        int level = 1,
        int streak = 0,
        DateOnly? lastCompletionDate = null)
    {
        return new DOJO2.Domain.Entities.AppUser
        {
            Id = id,
            UserName = $"user{ id }",
            Email = $"u{id}@test",
            ExpPoints = exp,
            Level = level,
            CurrentStreak = streak,
            LastCompletionDate = lastCompletionDate,
            CreatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task GetHeroStatus_ReturnsInitialValues()
    {
        using var context = CreateContext();
        var user = CreateUser(TestUserId, exp: 0, level: 1);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.GetHeroStatusAsync(TestUserId);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data!.Level);
        Assert.Equal(0, result.Data.ExpPoints);
        Assert.Equal(300, result.Data.ExpToNextLevel);
        Assert.Equal(0, result.Data.ProgressPercent);
        Assert.Equal(300, result.Data.ExpToLevelRemaining);
        Assert.Equal(0, result.Data.CurrentStreak);
        Assert.Equal("Почни серію сьогодні", result.Data.StreakText);
    }

    [Fact]
    public async Task AwardExpForTask_Todo_Adds50AndMarksXpAwarded()
    {
        using var context = CreateContext();
        var user = CreateUser(TestUserId);
        context.Users.Add(user);
        var todo = CreateTaskItem(TestUserId, "t1", isPlan: false);
        context.Tasks.Add(todo);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.AwardExpForTaskAsync(todo.Id, TestUserId);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        var updatedUser = await context.Users.FindAsync(TestUserId);
        var updatedTask = await context.Tasks.FindAsync(todo.Id);
        Assert.Equal(50, updatedUser!.ExpPoints);
        Assert.True(updatedTask!.XpAwarded);
        Assert.False(result.Data!.HasLeveledUp);
        Assert.Equal(250, result.Data.ExpToLevelRemaining);
        Assert.Equal(1, result.Data.CurrentStreak);
        Assert.Equal("1 день підряд", result.Data.StreakText);
    }

    [Fact]
    public async Task AwardExpForTask_Plan_Adds100()
    {
        using var context = CreateContext();
        var user = CreateUser(TestUserId);
        context.Users.Add(user);
        var plan = CreateTaskItem(TestUserId, "p1", isPlan: true);
        context.Tasks.Add(plan);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.AwardExpForTaskAsync(plan.Id, TestUserId);

        Assert.True(result.Success);
        var updatedUser = await context.Users.FindAsync(TestUserId);
        var updatedTask = await context.Tasks.FindAsync(plan.Id);
        Assert.Equal(100, updatedUser!.ExpPoints);
        Assert.True(updatedTask!.XpAwarded);
        Assert.False(result.Data!.HasLeveledUp);
        Assert.Equal(200, result.Data.ExpToLevelRemaining);
        Assert.Equal(1, result.Data.CurrentStreak);
        Assert.Equal("1 день підряд", result.Data.StreakText);
    }

    [Fact]
    public async Task AwardExpForTask_PreventsDoubleAward()
    {
        using var context = CreateContext();
        var user = CreateUser(TestUserId);
        context.Users.Add(user);
        var todo = CreateTaskItem(TestUserId, "t1");
        context.Tasks.Add(todo);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var first = await service.AwardExpForTaskAsync(todo.Id, TestUserId);
        Assert.True(first.Success);

        var second = await service.AwardExpForTaskAsync(todo.Id, TestUserId);
        Assert.False(second.Success);
        Assert.Contains("XP вже нараховано", second.Message);

        var updatedUser = await context.Users.FindAsync(TestUserId);
        Assert.Equal(50, updatedUser!.ExpPoints);
    }

    [Fact]
    public async Task AwardExpForTask_IncrementsStreak_WhenLastCompletionWasYesterday()
    {
        using var context = CreateContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var user = CreateUser(TestUserId, streak: 2, lastCompletionDate: today.AddDays(-1));
        context.Users.Add(user);
        var todo = CreateTaskItem(TestUserId, "t1");
        context.Tasks.Add(todo);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.AwardExpForTaskAsync(todo.Id, TestUserId);

        Assert.True(result.Success);
        var updatedUser = await context.Users.FindAsync(TestUserId);
        Assert.Equal(3, updatedUser!.CurrentStreak);
        Assert.Equal(today, updatedUser.LastCompletionDate);
        Assert.Equal(3, result.Data!.CurrentStreak);
        Assert.Equal("3 дні підряд", result.Data.StreakText);
    }

    [Fact]
    public async Task AwardExpForTask_DoesNotIncrementStreak_WhenLastCompletionWasToday()
    {
        using var context = CreateContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var user = CreateUser(TestUserId, streak: 4, lastCompletionDate: today);
        context.Users.Add(user);
        var todo = CreateTaskItem(TestUserId, "t1");
        context.Tasks.Add(todo);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.AwardExpForTaskAsync(todo.Id, TestUserId);

        Assert.True(result.Success);
        var updatedUser = await context.Users.FindAsync(TestUserId);
        Assert.Equal(4, updatedUser!.CurrentStreak);
        Assert.Equal(today, updatedUser.LastCompletionDate);
        Assert.Equal(4, result.Data!.CurrentStreak);
        Assert.Equal("4 дні підряд", result.Data.StreakText);
    }

    [Fact]
    public async Task AwardExpForTask_ResetsStreak_WhenGapExceedsOneDay()
    {
        using var context = CreateContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var user = CreateUser(TestUserId, streak: 5, lastCompletionDate: today.AddDays(-3));
        context.Users.Add(user);
        var todo = CreateTaskItem(TestUserId, "t1");
        context.Tasks.Add(todo);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.AwardExpForTaskAsync(todo.Id, TestUserId);

        Assert.True(result.Success);
        var updatedUser = await context.Users.FindAsync(TestUserId);
        Assert.Equal(1, updatedUser!.CurrentStreak);
        Assert.Equal(today, updatedUser.LastCompletionDate);
        Assert.Equal(1, result.Data!.CurrentStreak);
        Assert.Equal("1 день підряд", result.Data.StreakText);
    }

    [Fact]
    public async Task AwardExpForTask_LevelUp_SingleLevel()
    {
        using var context = CreateContext();
        var user = CreateUser(TestUserId, exp: 260, level: 1);
        context.Users.Add(user);
        var plan = CreateTaskItem(TestUserId, "p1", isPlan: true); // +100 -> 360
        context.Tasks.Add(plan);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.AwardExpForTaskAsync(plan.Id, TestUserId);

        Assert.True(result.Success);
        Assert.True(result.Data!.HasLeveledUp);
        Assert.Equal(1, result.Data.LevelsGained);
        var updatedUser = await context.Users.FindAsync(TestUserId);
        Assert.Equal(360, updatedUser!.ExpPoints);
        Assert.Equal(2, updatedUser.Level);
        Assert.Equal(240, result.Data.ExpToLevelRemaining);
    }

    [Fact]
    public async Task AwardExpForTask_LevelUp_MultiLevel()
    {
        using var context = CreateContext();
        var user = CreateUser(TestUserId, exp: 650, level: 1); // 650 +100 = 750 -> level 3
        context.Users.Add(user);
        var plan = CreateTaskItem(TestUserId, "p1", isPlan: true);
        context.Tasks.Add(plan);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.AwardExpForTaskAsync(plan.Id, TestUserId);

        Assert.True(result.Success);
        Assert.True(result.Data!.HasLeveledUp);
        Assert.Equal(2, result.Data.LevelsGained);
        var updatedUser = await context.Users.FindAsync(TestUserId);
        Assert.Equal(750, updatedUser!.ExpPoints);
        Assert.Equal(3, updatedUser.Level);
    }

    [Fact]
    public async Task AwardExpForTask_ReturnsFailure_WhenTaskNotFound()
    {
        using var context = CreateContext();
        var user = CreateUser(TestUserId);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.AwardExpForTaskAsync(999, TestUserId);

        Assert.False(result.Success);
        Assert.Contains("не знайдено", result.Message);
    }

    [Fact]
    public async Task GetHeroStatus_ResetsStreak_WhenCompletionIsTooOld()
    {
        using var context = CreateContext();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var user = CreateUser(TestUserId, streak: 7, lastCompletionDate: today.AddDays(-4));
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.GetHeroStatusAsync(TestUserId);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(0, result.Data!.CurrentStreak);
        Assert.Equal("Почни серію сьогодні", result.Data.StreakText);

        var updatedUser = await context.Users.FindAsync(TestUserId);
        Assert.Equal(0, updatedUser!.CurrentStreak);
    }
}

