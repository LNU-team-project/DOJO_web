using System.Linq.Expressions;
using System.Text;
using DOJO2.Application.ViewModels;
using DOJO2.Domain.Entities;
using DOJO2.Infrastructure.Data;
using DOJO2.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace DOJO_web.Tests;

public class UserServiceExportTests
{
    private sealed class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        public TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<TEntity>(expression);

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new TestAsyncEnumerable<TElement>(expression);

        public object? Execute(Expression expression) => _inner.Execute(expression);

        public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            if (typeof(TResult).IsGenericType && typeof(TResult).GetGenericTypeDefinition() == typeof(Task<>))
            {
                var resultType = typeof(TResult).GetGenericArguments()[0];
                var executeResult = _inner.Execute(expression);
                var fromResult = typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(resultType);
                return (TResult)fromResult.Invoke(null, new[] { executeResult })!;
            }

            return Execute<TResult>(expression);
        }
    }

    private sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(Expression expression) : base(expression) { }

        IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken)
            => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    private sealed class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public T Current => _inner.Current;

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync() => new(_inner.MoveNext());
    }

    private static Mock<DbSet<T>> BuildMockDbSet<T>(IList<T> source) where T : class
    {
        var queryable = source.AsQueryable();
        var dbSet = new Mock<DbSet<T>>();
        dbSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(queryable.Provider));
        dbSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        dbSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        dbSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());
        dbSet.As<IAsyncEnumerable<T>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));

        return dbSet;
    }

    private static UserManager<AppUser> BuildUserManager(IList<AppUser> users)
    {
        var store = new Mock<IQueryableUserStore<AppUser>>(MockBehavior.Loose);
        store.SetupGet(s => s.Users).Returns(BuildMockDbSet(users).Object);

        var options = new OptionsWrapper<IdentityOptions>(new IdentityOptions());
        var passwordHasher = new PasswordHasher<AppUser>();
        var userValidators = new[] { new UserValidator<AppUser>() };
        var passwordValidators = new[] { new PasswordValidator<AppUser>() };
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var services = new Mock<IServiceProvider>().Object;
        var logger = new Logger<UserManager<AppUser>>(new LoggerFactory());

        return new UserManager<AppUser>(
            store.Object,
            options,
            passwordHasher,
            userValidators,
            passwordValidators,
            keyNormalizer,
            errors,
            services,
            logger);
    }

    private static UserService BuildService(IAppDbContext context, UserManager<AppUser> userManager)
    {
        var logger = new Mock<ILogger<UserService>>();
        var env = new Mock<IWebHostEnvironment>();

        return new UserService(userManager, logger.Object, env.Object, context);
    }

    private static Mock<IAppDbContext> BuildContext(IList<TaskItem> tasks, IList<Pomodoro> pomodoros)
    {
        var contextMock = new Mock<IAppDbContext>(MockBehavior.Strict);
        contextMock.Setup(c => c.Tasks).Returns(BuildMockDbSet(tasks).Object);
        contextMock.Setup(c => c.Pomodoros).Returns(BuildMockDbSet(pomodoros).Object);
        return contextMock;
    }

    [Fact]
    public async Task ExportUserProfileCsvAsync_ReturnsFailure_WhenNoFieldsSelected()
    {
        var users = new List<AppUser>
        {
            BuildUser(1, "user", "user@example.com", 3, 120, 5)
        };
        var userManager = BuildUserManager(users);
        var context = BuildContext(new List<TaskItem>(), new List<Pomodoro>());
        var service = BuildService(context.Object, userManager);

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
        var userManager = BuildUserManager(new List<AppUser>());
        var context = BuildContext(new List<TaskItem>(), new List<Pomodoro>());
        var service = BuildService(context.Object, userManager);

        var result = await service.ExportUserProfileCsvAsync(99, new ProfileExportRequestViewModel());

        Assert.False(result.Success);
        Assert.Contains("не знайдено", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExportUserProfileCsvAsync_ReturnsCsv_WithSelectedStatistics()
    {
        var userId = 1;
        var now = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);
        var users = new List<AppUser>
        {
            BuildUser(userId, "student", "student@example.com", level: 8, expPoints: 1450, currentStreak: 12)
        };
        var tasks = new List<TaskItem>
        {
            new() { UserId = userId, Title = "Completed plan", IsPlan = true, IsCompleted = true, CompletedAt = now, CreatedAt = now },
            new() { UserId = userId, Title = "Completed todo", IsPlan = false, IsCompleted = true, CompletedAt = now, CreatedAt = now, GoalId = null, ParentTaskId = null },
            new() { UserId = userId, Title = "Incomplete todo", IsPlan = false, IsCompleted = false, CreatedAt = now, GoalId = null, ParentTaskId = null }
        };
        var pomodoros = new List<Pomodoro>
        {
            new() { UserId = userId, StartTime = now.AddHours(-3), EndTime = now.AddHours(-3).AddMinutes(25), DurationMinutes = 25 },
            new() { UserId = userId, StartTime = now.AddHours(-2), EndTime = now.AddHours(-2).AddMinutes(30), DurationMinutes = 30 },
            new() { UserId = userId, StartTime = now.AddHours(-1), EndTime = now.AddHours(-1).AddMinutes(40), DurationMinutes = 40 }
        };

        var userManager = BuildUserManager(users);
        var context = BuildContext(tasks, pomodoros);
        var service = BuildService(context.Object, userManager);

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

    private static AppUser BuildUser(int id, string userName, string email, int level, int expPoints, int currentStreak)
    {
        return new AppUser
        {
            Id = id,
            UserName = userName,
            Email = email,
            Level = level,
            ExpPoints = expPoints,
            CurrentStreak = currentStreak
        };
    }
}
