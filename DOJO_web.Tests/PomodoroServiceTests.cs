using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using DOJO2.Domain.Entities;
using DOJO2.Infrastructure.Data;
using DOJO2.Infrastructure.Services;
using DOJO2.Application.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DOJO_web.Tests;

public class PomodoroServiceTests
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
        public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
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

        dbSet.Setup(d => d.Add(It.IsAny<T>())).Callback<T>(source.Add);
        return dbSet;
    }

    private static PomodoroService BuildService(
        List<Pomodoro> pomodoros,
        List<TaskItem> tasks,
        out Mock<IAppDbContext> contextMock)
    {
        var pomodoroSet = BuildMockDbSet(pomodoros);
        var taskSet = BuildMockDbSet(tasks);
        contextMock = new Mock<IAppDbContext>(MockBehavior.Strict);
        contextMock.Setup(c => c.Pomodoros).Returns(pomodoroSet.Object);
        contextMock.Setup(c => c.Tasks).Returns(taskSet.Object);
        contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var logger = new Mock<ILogger<PomodoroService>>();
        return new PomodoroService(contextMock.Object, logger.Object);
    }

    [Fact]
    public async Task CreateSessionAsync_ReturnsFailure_WhenModelIsNull()
    {
        var service = BuildService(new List<Pomodoro>(), new List<TaskItem>(), out _);

        var result = await service.CreateSessionAsync(1, null);

        Assert.False(result.Success);
        Assert.Contains("не може бути порожньою", result.Message);
    }

    [Fact]
    public async Task CreateSessionAsync_ReturnsFailure_WhenDurationInvalid()
    {
        var service = BuildService(new List<Pomodoro>(), new List<TaskItem>(), out _);
        var model = new PomodoroSessionCreateViewModel
        {
            DurationMinutes = 0,
            WorkCycles = 1,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddMinutes(25)
        };

        var result = await service.CreateSessionAsync(1, model);

        Assert.False(result.Success);
        Assert.Contains("більшою за 0", result.Message);
    }

    [Fact]
    public async Task CreateSessionAsync_ReturnsFailure_WhenWorkCyclesInvalid()
    {
        var service = BuildService(new List<Pomodoro>(), new List<TaskItem>(), out _);
        var model = new PomodoroSessionCreateViewModel
        {
            DurationMinutes = 25,
            WorkCycles = 0,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddMinutes(25)
        };

        var result = await service.CreateSessionAsync(1, model);

        Assert.False(result.Success);
        Assert.Contains("циклів", result.Message);
    }

    [Fact]
    public async Task CreateSessionAsync_ReturnsFailure_WhenEndBeforeStart()
    {
        var service = BuildService(new List<Pomodoro>(), new List<TaskItem>(), out _);
        var now = DateTime.UtcNow;
        var model = new PomodoroSessionCreateViewModel
        {
            DurationMinutes = 25,
            WorkCycles = 1,
            StartTime = now,
            EndTime = now.AddMinutes(-5)
        };

        var result = await service.CreateSessionAsync(1, model);

        Assert.False(result.Success);
        Assert.Contains("пізнішим", result.Message);
    }

    [Fact]
    public async Task CreateSessionAsync_ReturnsFailure_WhenTaskMissing()
    {
        var service = BuildService(new List<Pomodoro>(), new List<TaskItem>(), out _);
        var model = new PomodoroSessionCreateViewModel
        {
            DurationMinutes = 25,
            WorkCycles = 1,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddMinutes(25),
            TaskId = 42
        };

        var result = await service.CreateSessionAsync(1, model);

        Assert.False(result.Success);
        Assert.Contains("не знайдено", result.Message);
    }

    [Fact]
    public async Task CreateSessionAsync_PersistsPomodoro_AndReturnsStats()
    {
        var pomodoros = new List<Pomodoro>();
        var tasks = new List<TaskItem> { new() { Id = 7, UserId = 3, Title = "Task" } };
        var service = BuildService(pomodoros, tasks, out var contextMock);
        var start = new DateTime(2026, 4, 2, 10, 0, 0, DateTimeKind.Local);
        var end = start.AddMinutes(25);
        var model = new PomodoroSessionCreateViewModel
        {
            DurationMinutes = 25,
            WorkCycles = 2,
            StartTime = start,
            EndTime = end,
            TaskId = 7
        };

        var result = await service.CreateSessionAsync(3, model);

        Assert.True(result.Success);
        Assert.Single(pomodoros);
        var saved = pomodoros.Single();
        Assert.Equal(3, saved.UserId);
        Assert.Equal(7, saved.TaskId);
        Assert.Equal(DateTimeKind.Utc, saved.StartTime.Kind);
        Assert.Equal(DateTimeKind.Utc, saved.EndTime?.Kind);
        contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTodayStatsAsync_ReturnsZero_WhenNoSessions()
    {
        var service = BuildService(new List<Pomodoro>(), new List<TaskItem>(), out _);

        var result = await service.GetTodayStatsAsync(1, new DateTime(2026, 4, 2, 12, 0, 0, DateTimeKind.Utc));

        Assert.True(result.Success);
        Assert.Equal(0, result.Data?.CompletedFocusSessions);
        Assert.Equal(0, result.Data?.TotalFocusMinutes);
    }

    [Fact]
    public async Task GetTodayStatsAsync_AggregatesSessionsWithinDay()
    {
        var day = new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc);
        var pomodoros = new List<Pomodoro>
        {
            new()
            {
                UserId = 9,
                StartTime = day.AddHours(1),
                DurationMinutes = 25
            },
            new()
            {
                UserId = 9,
                StartTime = day.AddHours(5),
                DurationMinutes = 15
            },
            new()
            {
                UserId = 9,
                StartTime = day.AddDays(1).AddMinutes(1),
                DurationMinutes = 30
            },
            new()
            {
                UserId = 10,
                StartTime = day.AddHours(3),
                DurationMinutes = 50
            }
        };
        var service = BuildService(pomodoros, new List<TaskItem>(), out _);

        var result = await service.GetTodayStatsAsync(9, day.AddHours(10));

        Assert.True(result.Success);
        Assert.Equal(2, result.Data?.CompletedFocusSessions);
        Assert.Equal(40, result.Data?.TotalFocusMinutes);
    }
}

