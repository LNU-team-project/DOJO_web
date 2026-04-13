using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using DOJO2.Domain.Entities;
using DOJO2.Infrastructure.Data;
using DOJO2.Infrastructure.Services;

namespace DOJO_web.Tests;

public class StatisticsServiceTests1
{
    private sealed class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        public TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
            => new TestAsyncEnumerable<TEntity>(expression);

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            => new TestAsyncEnumerable<TElement>(expression);

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

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
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

    private static StatisticsService BuildService(
        List<Pomodoro> pomodoros,
        List<TaskItem> tasks)
    {
        var pomodoroSet = BuildMockDbSet(pomodoros);
        var taskSet = BuildMockDbSet(tasks);
        var contextMock = new Mock<IAppDbContext>(MockBehavior.Strict);
        contextMock.Setup(c => c.Pomodoros).Returns(pomodoroSet.Object);
        contextMock.Setup(c => c.Tasks).Returns(taskSet.Object);
        var logger = new Mock<ILogger<StatisticsService>>();
        return new StatisticsService(contextMock.Object, logger.Object);
    }

    [Fact]
    public async Task GetTodayStatisticsAsync_ReturnsSuccess_WithCompletedTodos()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var userId = 1;

        var tasks = new List<TaskItem>
        {
            new() { Id = 1, UserId = userId, Title = "Todo 1", IsCompleted = true, CompletedAt = now, IsPlan = false, GoalId = null, ParentTaskId = null, CreatedAt = now, Priority = 2 },
            new() { Id = 2, UserId = userId, Title = "Todo 2", IsCompleted = true, CompletedAt = now, IsPlan = false, GoalId = null, ParentTaskId = null, CreatedAt = now, Priority = 2 },
            new() { Id = 3, UserId = userId, Title = "Todo 3", IsCompleted = false, IsPlan = false, GoalId = null, ParentTaskId = null, CreatedAt = now, Priority = 2 }
        };

        var service = BuildService(new List<Pomodoro>(), tasks);

        // Act
        var result = await service.GetTodayStatisticsAsync(userId, now);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.CompletedTodos);
    }

    [Fact]
    public async Task GetTodayStatisticsAsync_ReturnsSuccess_WithCompletedPlans()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var userId = 1;

        var tasks = new List<TaskItem>
        {
            new() { Id = 1, UserId = userId, Title = "Plan 1", IsCompleted = true, CompletedAt = now, IsPlan = true, CreatedAt = now, Priority = 2, GoalId = null, ParentTaskId = null },
            new() { Id = 2, UserId = userId, Title = "Plan 2", IsCompleted = false, IsPlan = true, CompletedAt = null, CreatedAt = now, Priority = 2, GoalId = null, ParentTaskId = null }
        };

        var service = BuildService(new List<Pomodoro>(), tasks);

        // Act
        var result = await service.GetTodayStatisticsAsync(userId, now);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.CompletedPlans);
    }

    [Fact]
    public async Task GetTodayStatisticsAsync_ReturnsSuccess_WithPomodoros()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var userId = 1;

        var pomodoros = new List<Pomodoro>
        {
            new() { Id = 1, UserId = userId, StartTime = now, EndTime = now.AddMinutes(25), DurationMinutes = 25 },
            new() { Id = 2, UserId = userId, StartTime = now, EndTime = now.AddMinutes(25), DurationMinutes = 25 },
            new() { Id = 3, UserId = userId, StartTime = now, EndTime = now.AddMinutes(25), DurationMinutes = 25 }
        };

        var service = BuildService(pomodoros, new List<TaskItem>());

        // Act
        var result = await service.GetTodayStatisticsAsync(userId, now);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(3, result.Data.CompletedPomodoroSessions);
        Assert.Equal(75, result.Data.TotalPomodoroMinutes);
    }

    [Fact]
    public async Task GetTodayStatisticsAsync_ReturnsSuccess_WithEmptyData()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var userId = 1;

        var service = BuildService(new List<Pomodoro>(), new List<TaskItem>());

        // Act
        var result = await service.GetTodayStatisticsAsync(userId, now);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(0, result.Data.CompletedTodos);
        Assert.Equal(0, result.Data.CompletedPlans);
        Assert.Equal(0, result.Data.CompletedPomodoroSessions);
        Assert.Equal(0, result.Data.TotalPomodoroMinutes);
    }

    [Fact]
    public async Task GetDetailedStatisticsAsync_ReturnsSuccess_CalculatesCompletionRates()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var monthAgo = now.AddMonths(-1);
        var userId = 1;

        var tasks = new List<TaskItem>
        {
            new() { Id = 1, UserId = userId, Title = "Todo 1", IsCompleted = true, CompletedAt = now, IsPlan = false, GoalId = null, ParentTaskId = null, CreatedAt = monthAgo, Priority = 2 },
            new() { Id = 2, UserId = userId, Title = "Todo 2", IsCompleted = true, CompletedAt = now, IsPlan = false, GoalId = null, ParentTaskId = null, CreatedAt = monthAgo, Priority = 2 },
            new() { Id = 3, UserId = userId, Title = "Todo 3", IsCompleted = false, IsPlan = false, GoalId = null, ParentTaskId = null, CreatedAt = monthAgo, Priority = 2 },
            new() { Id = 4, UserId = userId, Title = "Todo 4", IsCompleted = false, IsPlan = false, GoalId = null, ParentTaskId = null, CreatedAt = monthAgo, Priority = 2 }
        };

        var service = BuildService(new List<Pomodoro>(), tasks);

        // Act
        var result = await service.GetDetailedStatisticsAsync(userId, monthAgo);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.CompletedTodos);
        Assert.Equal(4, result.Data.TotalTodos);
        Assert.Equal(50.0, result.Data.TodoCompletionRate);
    }

    [Fact]
    public async Task GetDetailedStatisticsAsync_ReturnsSuccess_WithZeroTasks()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var userId = 1;

        var service = BuildService(new List<Pomodoro>(), new List<TaskItem>());

        // Act
        var result = await service.GetDetailedStatisticsAsync(userId, now);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(0.0, result.Data.TodoCompletionRate);
        Assert.Equal(0.0, result.Data.PlanCompletionRate);
    }

    [Fact]
    public async Task GetDetailedStatisticsAsync_ReturnsSuccess_TracksLastCompletedDates()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var monthAgo = now.AddMonths(-1);
        var completedDate = now.AddDays(-1);
        var userId = 1;

        var tasks = new List<TaskItem>
        {
            new() { Id = 1, UserId = userId, Title = "Todo 1", IsCompleted = true, CompletedAt = completedDate, IsPlan = false, GoalId = null, ParentTaskId = null, CreatedAt = monthAgo, Priority = 2 }
        };

        var service = BuildService(new List<Pomodoro>(), tasks);

        // Act
        var result = await service.GetDetailedStatisticsAsync(userId, monthAgo);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.LastCompletedTodo);
        Assert.Equal(completedDate, result.Data.LastCompletedTodo);
    }

    [Fact]
    public async Task GetDetailedStatisticsAsync_ReturnsSuccess_WithPlans()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var monthAgo = now.AddMonths(-1);
        var userId = 1;

        var tasks = new List<TaskItem>
        {
            new() { Id = 1, UserId = userId, Title = "Plan 1", IsCompleted = true, CompletedAt = now, IsPlan = true, CreatedAt = monthAgo, Priority = 2 },
            new() { Id = 2, UserId = userId, Title = "Plan 2", IsCompleted = true, CompletedAt = now, IsPlan = true, CreatedAt = monthAgo, Priority = 2 },
            new() { Id = 3, UserId = userId, Title = "Plan 3", IsCompleted = false, IsPlan = true, CreatedAt = monthAgo, Priority = 2 }
        };

        var service = BuildService(new List<Pomodoro>(), tasks);

        // Act
        var result = await service.GetDetailedStatisticsAsync(userId, monthAgo);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.CompletedPlans);
        Assert.Equal(3, result.Data.TotalPlans);
        Assert.Equal(Math.Round(66.66666666666666, 1), result.Data.PlanCompletionRate);
    }
}
