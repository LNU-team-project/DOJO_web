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

    // ТЕСТИ GetWeeklyProgressAsync 

    [Fact]
    public async Task GetWeeklyProgressAsync_ReturnsSuccess_WithEmptyData()
    {
        // Arrange
        var dateInWeek = new DateTime(2026, 4, 14, 0, 0, 0, DateTimeKind.Utc); // Monday
        var userId = 1;

        var service = BuildService(new List<Pomodoro>(), new List<TaskItem>());

        // Act
        var result = await service.GetWeeklyProgressAsync(userId, dateInWeek);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(0, result.Data.TotalCompletedTodos);
        Assert.Equal(0, result.Data.TotalCompletedPlans);
        Assert.Equal(0, result.Data.TotalPomodoroSessions);
        Assert.Equal(0, result.Data.TotalPomodoroMinutes);
        Assert.Equal(0.0, result.Data.AverageTodosPerDay);
        Assert.Equal(0.0, result.Data.AveragePlansPerDay);
        Assert.Equal(0.0, result.Data.AveragePomodoroSessionsPerDay);
    }

    [Fact]
    public async Task GetWeeklyProgressAsync_ReturnsSuccess_WithCompletedTodos()
    {
        // Arrange
        var sunday = new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc); // Sunday
        var monday = sunday.AddDays(1);
        var tuesday = sunday.AddDays(2);
        var userId = 1;

        var tasks = new List<TaskItem>
        {
            new() { Id = 1, UserId = userId, Title = "Todo 1", IsCompleted = true, CompletedAt = monday, IsPlan = false, GoalId = null, ParentTaskId = null, CreatedAt = sunday, Priority = 2 },
            new() { Id = 2, UserId = userId, Title = "Todo 2", IsCompleted = true, CompletedAt = monday, IsPlan = false, GoalId = null, ParentTaskId = null, CreatedAt = sunday, Priority = 2 },
            new() { Id = 3, UserId = userId, Title = "Todo 3", IsCompleted = true, CompletedAt = tuesday, IsPlan = false, GoalId = null, ParentTaskId = null, CreatedAt = sunday, Priority = 2 }
        };

        var service = BuildService(new List<Pomodoro>(), tasks);

        // Act
        var result = await service.GetWeeklyProgressAsync(userId, monday);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(3, result.Data.TotalCompletedTodos);
        Assert.Equal(3, result.Data.DailyStats[1].CompletedTodos); // Monday has 2 todos
        Assert.Equal(2, result.Data.DailyStats[1].CompletedTodos);
        Assert.Equal(1, result.Data.DailyStats[2].CompletedTodos); // Tuesday has 1 
    }

    [Fact]
    public async Task GetWeeklyProgressAsync_ReturnsSuccess_WithCompletedPlans()
    {
        // Arrange
        var sunday = new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc);
        var wednesday = sunday.AddDays(3);
        var userId = 1;

        var tasks = new List<TaskItem>
        {
            new() { Id = 1, UserId = userId, Title = "Plan 1", IsCompleted = true, CompletedAt = wednesday, IsPlan = true, CreatedAt = sunday, Priority = 2, GoalId = null, ParentTaskId = null },
            new() { Id = 2, UserId = userId, Title = "Plan 2", IsCompleted = true, CompletedAt = wednesday, IsPlan = true, CreatedAt = sunday, Priority = 2, GoalId = null, ParentTaskId = null }
        };

        var service = BuildService(new List<Pomodoro>(), tasks);

        // Act
        var result = await service.GetWeeklyProgressAsync(userId, wednesday);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.TotalCompletedPlans);
        Assert.Equal(2, result.Data.DailyStats[3].CompletedPlans); // Wednesday is index 3
    }

    [Fact]
    public async Task GetWeeklyProgressAsync_ReturnsSuccess_WithPomodoros()
    {
        // Arrange
        var sunday = new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc);
        var monday = sunday.AddDays(1);
        var friday = sunday.AddDays(5);
        var userId = 1;

        var pomodoros = new List<Pomodoro>
        {
            new() { Id = 1, UserId = userId, StartTime = monday, EndTime = monday.AddMinutes(25), DurationMinutes = 25 },
            new() { Id = 2, UserId = userId, StartTime = monday, EndTime = monday.AddMinutes(25), DurationMinutes = 25 },
            new() { Id = 3, UserId = userId, StartTime = friday, EndTime = friday.AddMinutes(30), DurationMinutes = 30 },
            new() { Id = 4, UserId = userId, StartTime = friday, EndTime = friday.AddMinutes(25), DurationMinutes = 25 }
        };

        var service = BuildService(pomodoros, new List<TaskItem>());

        // Act
        var result = await service.GetWeeklyProgressAsync(userId, monday);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(4, result.Data.TotalPomodoroSessions);
        Assert.Equal(105, result.Data.TotalPomodoroMinutes);
        Assert.Equal(2, result.Data.DailyStats[1].PomodoroSessions); // Monday
        Assert.Equal(50, result.Data.DailyStats[1].TotalPomodoroMinutes); // Monday
        Assert.Equal(2, result.Data.DailyStats[5].PomodoroSessions); // Friday
        Assert.Equal(55, result.Data.DailyStats[5].TotalPomodoroMinutes); // Friday
    }

    [Fact]
    public async Task GetWeeklyProgressAsync_ReturnsSuccess_CalculatesAveragesCorrectly()
    {
        // Arrange
        const int DaysInWeek = 7;
        var sunday = new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc);
        var monday = sunday.AddDays(1);
        var userId = 1;

        var tasks = new List<TaskItem>
        {
            new() { Id = 1, UserId = userId, Title = "Todo 1", IsCompleted = true, CompletedAt = monday, IsPlan = false, GoalId = null, ParentTaskId = null, CreatedAt = sunday, Priority = 2 },
            new() { Id = 2, UserId = userId, Title = "Todo 2", IsCompleted = true, CompletedAt = monday.AddDays(1), IsPlan = false, GoalId = null, ParentTaskId = null, CreatedAt = sunday, Priority = 2 },
            new() { Id = 3, UserId = userId, Title = "Plan 1", IsCompleted = true, CompletedAt = monday.AddDays(2), IsPlan = true, CreatedAt = sunday, Priority = 2, GoalId = null, ParentTaskId = null }
        };

        var pomodoros = new List<Pomodoro>
        {
            new() { Id = 1, UserId = userId, StartTime = monday.AddDays(3), EndTime = monday.AddDays(3).AddMinutes(25), DurationMinutes = 25 },
            new() { Id = 2, UserId = userId, StartTime = monday.AddDays(3), EndTime = monday.AddDays(3).AddMinutes(25), DurationMinutes = 25 },
            new() { Id = 3, UserId = userId, StartTime = monday.AddDays(4), EndTime = monday.AddDays(4).AddMinutes(25), DurationMinutes = 25 }
        };

        var service = BuildService(pomodoros, tasks);

        // Act
        var result = await service.GetWeeklyProgressAsync(userId, monday);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        var expectedAvgTodos = Math.Round(2.0 / DaysInWeek, 1);
        var expectedAvgPlans = Math.Round(1.0 / DaysInWeek, 1);
        var expectedAvgPomodoros = Math.Round(3.0 / DaysInWeek, 1);

        Assert.Equal(expectedAvgTodos, result.Data.AverageTodosPerDay);
        Assert.Equal(expectedAvgPlans, result.Data.AveragePlansPerDay);
        Assert.Equal(expectedAvgPomodoros, result.Data.AveragePomodoroSessionsPerDay);
    }

    [Fact]
    public async Task GetWeeklyProgressAsync_ReturnsSuccess_DailyStatsHaveCorrectDates()
    {
        // Arrange
        var sunday = new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc);
        var userId = 1;

        var service = BuildService(new List<Pomodoro>(), new List<TaskItem>());

        // Act
        var result = await service.GetWeeklyProgressAsync(userId, sunday);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(7, result.Data.DailyStats.Count);
        
        for (int i = 0; i < 7; i++)
        {
            var expectedDate = sunday.AddDays(i);
            Assert.Equal(expectedDate.Date, result.Data.DailyStats[i].Date.Date);
            Assert.Equal(i, result.Data.DailyStats[i].DayOfWeek);
        }
    }

    [Fact]
    public async Task GetWeeklyProgressAsync_ReturnsSuccess_DayNamesAreCorrect()
    {
        // Arrange
        var sunday = new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc);
        var userId = 1;
        var expectedDayNames = new[] { "Нд", "Пн", "Вт", "Ср", "Чт", "Пт", "Сб" };

        var service = BuildService(new List<Pomodoro>(), new List<TaskItem>());

        // Act
        var result = await service.GetWeeklyProgressAsync(userId, sunday);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        
        for (int i = 0; i < 7; i++)
        {
            Assert.Equal(expectedDayNames[i], result.Data.DailyStats[i].DayName);
        }
    }

    [Fact]
    public async Task GetWeeklyProgressAsync_ReturnsSuccess_WeekStartAndEndDates()
    {
        // Arrange
        var dateInWeek = new DateTime(2026, 4, 14, 0, 0, 0, DateTimeKind.Utc); // Tuesday
        var userId = 1;
        var expectedWeekStart = new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc); // Sunday
        var expectedWeekEnd = new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc); // Sunday of next week

        var service = BuildService(new List<Pomodoro>(), new List<TaskItem>());

        // Act
        var result = await service.GetWeeklyProgressAsync(userId, dateInWeek);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(expectedWeekStart, result.Data.WeekStartDate);
        Assert.Equal(expectedWeekEnd, result.Data.WeekEndDate);
    }

    [Fact]
    public async Task GetWeeklyProgressAsync_ReturnsSuccess_FiltersOnlyCompletedTasks()
    {
        // Arrange
        var sunday = new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc);
        var monday = sunday.AddDays(1);
        var userId = 1;

        var tasks = new List<TaskItem>
        {
            new() { Id = 1, UserId = userId, Title = "Todo 1", IsCompleted = true, CompletedAt = monday, IsPlan = false, GoalId = null, ParentTaskId = null, CreatedAt = sunday, Priority = 2 },
            new() { Id = 2, UserId = userId, Title = "Todo 2", IsCompleted = false, CompletedAt = null, IsPlan = false, GoalId = null, ParentTaskId = null, CreatedAt = sunday, Priority = 2 },
            new() { Id = 3, UserId = userId, Title = "Todo 3", IsCompleted = false, CompletedAt = null, IsPlan = false, GoalId = null, ParentTaskId = null, CreatedAt = sunday, Priority = 2 }
        };

        var service = BuildService(new List<Pomodoro>(), tasks);

        // Act
        var result = await service.GetWeeklyProgressAsync(userId, monday);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.TotalCompletedTodos);
        Assert.Equal(1, result.Data.DailyStats[1].CompletedTodos);
    }

    [Fact]
    public async Task GetWeeklyProgressAsync_ReturnsSuccess_ExcludesPlanTasksFromTodos()
    {
        // Arrange
        var sunday = new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc);
        var monday = sunday.AddDays(1);
        var userId = 1;

        var tasks = new List<TaskItem>
        {
            new() { Id = 1, UserId = userId, Title = "Todo 1", IsCompleted = true, CompletedAt = monday, IsPlan = false, GoalId = null, ParentTaskId = null, CreatedAt = sunday, Priority = 2 },
            new() { Id = 2, UserId = userId, Title = "Plan 1", IsCompleted = true, CompletedAt = monday, IsPlan = true, CreatedAt = sunday, Priority = 2, GoalId = null, ParentTaskId = null }
        };

        var service = BuildService(new List<Pomodoro>(), tasks);

        // Act
        var result = await service.GetWeeklyProgressAsync(userId, monday);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.TotalCompletedTodos);
        Assert.Equal(1, result.Data.TotalCompletedPlans);
    }

    [Fact]
    public async Task GetWeeklyProgressAsync_ReturnsSuccess_IgnoresTasksWithGoalId()
    {
        // Arrange
        var sunday = new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc);
        var monday = sunday.AddDays(1);
        var userId = 1;

        var tasks = new List<TaskItem>
        {
            new() { Id = 1, UserId = userId, Title = "Todo 1", IsCompleted = true, CompletedAt = monday, IsPlan = false, GoalId = null, ParentTaskId = null, CreatedAt = sunday, Priority = 2 },
            new() { Id = 2, UserId = userId, Title = "Todo 2", IsCompleted = true, CompletedAt = monday, IsPlan = false, GoalId = 1, ParentTaskId = null, CreatedAt = sunday, Priority = 2 }
        };

        var service = BuildService(new List<Pomodoro>(), tasks);

        // Act
        var result = await service.GetWeeklyProgressAsync(userId, monday);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.TotalCompletedTodos);
    }

    [Fact]
    public async Task GetWeeklyProgressAsync_ReturnsSuccess_IgnoresSubtasks()
    {
        // Arrange
        var sunday = new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc);
        var monday = sunday.AddDays(1);
        var userId = 1;

        var tasks = new List<TaskItem>
        {
            new() { Id = 1, UserId = userId, Title = "Todo 1", IsCompleted = true, CompletedAt = monday, IsPlan = false, GoalId = null, ParentTaskId = null, CreatedAt = sunday, Priority = 2 },
            new() { Id = 2, UserId = userId, Title = "Subtask", IsCompleted = true, CompletedAt = monday, IsPlan = false, GoalId = null, ParentTaskId = 1, CreatedAt = sunday, Priority = 2 }
        };

        var service = BuildService(new List<Pomodoro>(), tasks);

        // Act
        var result = await service.GetWeeklyProgressAsync(userId, monday);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.TotalCompletedTodos);
    }

    [Fact]
    public async Task GetWeeklyProgressAsync_ReturnsSuccess_FiltersByUserId()
    {
        // Arrange
        var sunday = new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc);
        var monday = sunday.AddDays(1);
        var userId1 = 1;
        var userId2 = 2;

        var tasks = new List<TaskItem>
        {
            new() { Id = 1, UserId = userId1, Title = "Todo 1", IsCompleted = true, CompletedAt = monday, IsPlan = false, GoalId = null, ParentTaskId = null, CreatedAt = sunday, Priority = 2 },
            new() { Id = 2, UserId = userId2, Title = "Todo 2", IsCompleted = true, CompletedAt = monday, IsPlan = false, GoalId = null, ParentTaskId = null, CreatedAt = sunday, Priority = 2 }
        };

        var service = BuildService(new List<Pomodoro>(), tasks);

        // Act
        var result = await service.GetWeeklyProgressAsync(userId1, monday);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.TotalCompletedTodos);
    }

    [Fact]
    public async Task GetWeeklyProgressAsync_ReturnsSuccess_WithMixedDataAcrossWeek()
    {
        // Arrange
        var sunday = new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc);
        var userId = 1;
        var tasks = new List<TaskItem>();
        var pomodoros = new List<Pomodoro>();

        // Додаємо завдання на кожен день тижня
        for (int i = 0; i < 7; i++)
        {
            var dayDate = sunday.AddDays(i);
            tasks.Add(new() { Id = i + 1, UserId = userId, Title = $"Todo {i + 1}", IsCompleted = true, CompletedAt = dayDate, IsPlan = false, GoalId = null, ParentTaskId = null, CreatedAt = sunday, Priority = 2 });
            tasks.Add(new() { Id = 100 + i + 1, UserId = userId, Title = $"Plan {i + 1}", IsCompleted = true, CompletedAt = dayDate, IsPlan = true, CreatedAt = sunday, Priority = 2, GoalId = null, ParentTaskId = null });
            pomodoros.Add(new() { Id = i + 1, UserId = userId, StartTime = dayDate, EndTime = dayDate.AddMinutes(25), DurationMinutes = 25 });
        }

        var service = BuildService(pomodoros, tasks);

        // Act
        var result = await service.GetWeeklyProgressAsync(userId, sunday);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(7, result.Data.TotalCompletedTodos);
        Assert.Equal(7, result.Data.TotalCompletedPlans);
        Assert.Equal(7, result.Data.TotalPomodoroSessions);
        Assert.Equal(175, result.Data.TotalPomodoroMinutes);
        Assert.Equal(1.0, result.Data.AverageTodosPerDay);
        Assert.Equal(1.0, result.Data.AveragePlansPerDay);
        Assert.Equal(1.0, result.Data.AveragePomodoroSessionsPerDay);

        // Перевіряємо дневну статистику
        for (int i = 0; i < 7; i++)
        {
            Assert.Equal(1, result.Data.DailyStats[i].CompletedTodos);
            Assert.Equal(1, result.Data.DailyStats[i].CompletedPlans);
            Assert.Equal(1, result.Data.DailyStats[i].PomodoroSessions);
            Assert.Equal(25, result.Data.DailyStats[i].TotalPomodoroMinutes);
        }
    }

    [Fact]
    public async Task GetWeeklyProgressAsync_ReturnsSuccess_DefaultsToCurrentWeek()
    {
        // Arrange
        var userId = 1;

        var service = BuildService(new List<Pomodoro>(), new List<TaskItem>());

        // Act
        var result = await service.GetWeeklyProgressAsync(userId, null);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(7, result.Data.DailyStats.Count);
    }

    [Fact]
    public async Task GetWeeklyProgressAsync_ReturnsSuccess_HandlesWeekBoundaryCorrectly()
    {
        // Arrange
        var saturday = new DateTime(2026, 4, 18, 0, 0, 0, DateTimeKind.Utc); // Saturday
        var sunday = saturday.AddDays(1); // Sunday of next week
        var userId = 1;

        // Завдання на суботу та неділю
        var tasks = new List<TaskItem>
        {
            new() { Id = 1, UserId = userId, Title = "Todo Saturday", IsCompleted = true, CompletedAt = saturday, IsPlan = false, GoalId = null, ParentTaskId = null, CreatedAt = saturday, Priority = 2 },
            new() { Id = 2, UserId = userId, Title = "Todo Sunday", IsCompleted = true, CompletedAt = sunday, IsPlan = false, GoalId = null, ParentTaskId = null, CreatedAt = sunday, Priority = 2 }
        };

        var service = BuildService(new List<Pomodoro>(), tasks);

        // Act
        var resultWeek1 = await service.GetWeeklyProgressAsync(userId, saturday);
        var resultWeek2 = await service.GetWeeklyProgressAsync(userId, sunday);

        // Assert
        Assert.True(resultWeek1.Success);
        Assert.True(resultWeek2.Success);
        Assert.NotNull(resultWeek1.Data);
        Assert.NotNull(resultWeek2.Data);
        
        // На першому тижні тільки субота має завдання
        Assert.Equal(1, resultWeek1.Data.TotalCompletedTodos);
        Assert.Equal(1, resultWeek1.Data.DailyStats[6].CompletedTodos); // Saturday is index 6
        
        // На другому тижні тільки неділя має завдання
        Assert.Equal(1, resultWeek2.Data.TotalCompletedTodos);
        Assert.Equal(1, resultWeek2.Data.DailyStats[0].CompletedTodos); // Sunday is index 0
    }
}
