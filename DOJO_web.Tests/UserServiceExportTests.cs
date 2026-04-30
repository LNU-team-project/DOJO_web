using System.Text;
using DOJO2.Application.ViewModels;
using DOJO2.Domain.Entities;
using DOJO2.Infrastructure.Data;
using DOJO2.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace DOJO_web.Tests;

public class UserServiceExportTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static UserManager<AppUser> BuildUserManager(AppDbContext context)
    {
        var store = new UserStore<AppUser, IdentityRole<int>, AppDbContext, int>(context);
        var options = new OptionsWrapper<IdentityOptions>(new IdentityOptions());
        var passwordHasher = new PasswordHasher<AppUser>();
        var userValidators = new[] { new UserValidator<AppUser>() };
        var passwordValidators = new[] { new PasswordValidator<AppUser>() };
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var services = new ServiceCollection().BuildServiceProvider();
        var logger = new Logger<UserManager<AppUser>>(new LoggerFactory());

        return new UserManager<AppUser>(
            store,
            options,
            passwordHasher,
            userValidators,
            passwordValidators,
            keyNormalizer,
            errors,
            services,
            logger);
    }

    private static UserService BuildService(AppDbContext context, UserManager<AppUser> userManager)
    {
        var logger = new Mock<ILogger<UserService>>();
        var env = new Mock<IWebHostEnvironment>();

        return new UserService(userManager, logger.Object, env.Object, context);
    }

    private static AppUser BuildUser(int id, string userName, string email, int level, int expPoints, int currentStreak)
    {
        return new AppUser
        {
            Id = id,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            Level = level,
            ExpPoints = expPoints,
            CurrentStreak = currentStreak
        };
    }

    [Fact]
    public async Task ExportUserProfileCsvAsync_ReturnsFailure_WhenNoFieldsSelected()
    {
        using var context = CreateContext();
        context.Users.Add(BuildUser(1, "user", "user@example.com", 3, 120, 5));
        await context.SaveChangesAsync();

        var userManager = BuildUserManager(context);
        var service = BuildService(context, userManager);

        var result = await service.ExportUserProfileCsvAsync(1, new ProfileExportRequestViewModel
        {
            IncludeLevel = false,
            IncludeExpPoints = false,
            IncludeCurrentStreak = false,
            IncludeCompletedPlans = false,
            IncludeCompletedTasks = false,
            IncludePomodoroSessions = false,
            IncludeFocusMinutes = false
        });

        Assert.False(result.Success);
        Assert.Contains("хоча б один параметр", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExportUserProfileCsvAsync_ReturnsFailure_WhenUserMissing()
    {
        using var context = CreateContext();
        var userManager = BuildUserManager(context);
        var service = BuildService(context, userManager);

        var result = await service.ExportUserProfileCsvAsync(99, new ProfileExportRequestViewModel());

        Assert.False(result.Success);
        Assert.Contains("не знайдено", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExportUserProfileCsvAsync_ReturnsCsv_WithSelectedStatistics()
    {
        using var context = CreateContext();
        var userId = 1;
        context.Users.Add(BuildUser(userId, "student", "student@example.com", level: 8, expPoints: 1450, currentStreak: 12));
        context.Tasks.AddRange(
            new TaskItem { UserId = userId, Title = "Completed plan", IsPlan = true, IsCompleted = true, CompletedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
            new TaskItem { UserId = userId, Title = "Completed todo", IsPlan = false, IsCompleted = true, CompletedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow, GoalId = null, ParentTaskId = null },
            new TaskItem { UserId = userId, Title = "Incomplete todo", IsPlan = false, IsCompleted = false, CreatedAt = DateTime.UtcNow, GoalId = null, ParentTaskId = null }
        );
        context.Pomodoros.AddRange(
            new Pomodoro { UserId = userId, StartTime = DateTime.UtcNow.AddHours(-3), EndTime = DateTime.UtcNow.AddHours(-3).AddMinutes(25), DurationMinutes = 25 },
            new Pomodoro { UserId = userId, StartTime = DateTime.UtcNow.AddHours(-2), EndTime = DateTime.UtcNow.AddHours(-2).AddMinutes(30), DurationMinutes = 30 },
            new Pomodoro { UserId = userId, StartTime = DateTime.UtcNow.AddHours(-1), EndTime = DateTime.UtcNow.AddHours(-1).AddMinutes(40), DurationMinutes = 40 }
        );
        await context.SaveChangesAsync();

        var userManager = BuildUserManager(context);
        var service = BuildService(context, userManager);

        var result = await service.ExportUserProfileCsvAsync(userId, new ProfileExportRequestViewModel
        {
            IncludeLevel = true,
            IncludeExpPoints = true,
            IncludeCurrentStreak = true,
            IncludeCompletedPlans = true,
            IncludeCompletedTasks = true,
            IncludePomodoroSessions = true,
            IncludeFocusMinutes = true
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.EndsWith(".csv", result.Data!.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("text/csv; charset=utf-8", result.Data.ContentType);

        var csv = Encoding.UTF8.GetString(result.Data.Content).TrimStart('\uFEFF').Trim();
        Assert.Contains("Рівень користувача;Очки досвіду;Серія;Скільки всього виконаних планів;Скільки виконано завдань;Скільки було сесій помодоро;Скільки було хвилин фокусу", csv);
        Assert.Contains("8;1450;12;1;1;3;95", csv);
    }
}


