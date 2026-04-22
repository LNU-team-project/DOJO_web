using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using DOJO2.Application.Interfaces;
using DOJO2.Domain.Entities;
using DOJO2.Infrastructure.Data;
using DOJO2.Infrastructure.Services;
using DOJO2.Application.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using Xunit;

namespace DOJO_web.Tests;

public class PlanServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

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

    private static IPlanService BuildPlanServiceWithPlans(List<TaskItem> plans, out Mock<IAppDbContext> contextMock)
    {
        var dbSetMock = BuildMockDbSet(plans);
        contextMock = new Mock<IAppDbContext>(MockBehavior.Strict);
        contextMock.Setup(c => c.Tasks).Returns(dbSetMock.Object);
        contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return new PlanService(contextMock.Object);
    }

    [Fact]
    public async Task CreatePlanAsync_ReturnsFailure_WhenModelIsNull()
    {
        using var context = CreateContext();
        var service = new PlanService(context);

        var result = await service.CreatePlanAsync(1, null);

        Assert.False(result.Success);
        Assert.Contains("не може бути порожною", result.Message);
    }

    [Fact]
    public async Task CreatePlanAsync_ReturnsFailure_WhenTitleEmpty()
    {
        using var context = CreateContext();
        var service = new PlanService(context);
        var model = new PlanCreateViewModel { Title = "   ", ScheduledAt = DateTime.UtcNow };

        var result = await service.CreatePlanAsync(1, model);

        Assert.False(result.Success);
        Assert.Contains("Назва плану не може бути порожньою", result.Message);
    }

    [Fact]
    public async Task CreatePlanAsync_ReturnsFailure_WhenScheduledAtMissing()
    {
        using var context = CreateContext();
        var service = new PlanService(context);
        var model = new PlanCreateViewModel { Title = "Plan", ScheduledAt = null };

        var result = await service.CreatePlanAsync(1, model);

        Assert.False(result.Success);
        Assert.Contains("Оберіть дату та час плану", result.Message);
    }

    [Fact]
    public async Task CreatePlanAsync_Succeeds_AndPersistsPlan()
    {
        using var context = CreateContext();
        var service = new PlanService(context);
        var now = DateTime.UtcNow;
        var model = new PlanCreateViewModel
        {
            Title = "Test plan",
            Description = "Desc",
            Priority = 2,
            ScheduledAt = now
        };

        var result = await service.CreatePlanAsync(7, model);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("Test plan", result.Data!.Title);
        Assert.Equal(2, result.Data.Priority);
        Assert.Equal(now, result.Data.ScheduledAt);

        var saved = await context.Tasks.FirstOrDefaultAsync(t => t.Id == result.Data.Id);
        Assert.NotNull(saved);
        Assert.Equal(7, saved!.UserId);
        Assert.True(saved.IsPlan);
    }

    [Fact]
    public async Task GetUserPlansAsync_ReturnsSeparatedLists()
    {
        using var context = CreateContext();
        context.Tasks.AddRange(
            new DOJO2.Domain.Entities.TaskItem { UserId = 1, Title = "active", IsPlan = true, ScheduledAt = DateTime.UtcNow, IsCompleted = false },
            new DOJO2.Domain.Entities.TaskItem { UserId = 1, Title = "done", IsPlan = true, ScheduledAt = DateTime.UtcNow, IsCompleted = true, CompletedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var service = new PlanService(context);
        var result = await service.GetUserPlansAsync(1);

        Assert.True(result.Success);
        Assert.Single(result.Data!.IncompletePlans);
        Assert.Single(result.Data.CompletedPlans);
        Assert.Equal("active", result.Data.IncompletePlans[0].Title);
        Assert.Equal("done", result.Data.CompletedPlans[0].Title);
    }

    [Fact]
    public async Task MarkPlanAsCompleted_SetsFlags()
    {
        using var context = CreateContext();
        var plan = new DOJO2.Domain.Entities.TaskItem { UserId = 1, Title = "p", IsPlan = true, IsCompleted = false };
        context.Tasks.Add(plan);
        await context.SaveChangesAsync();

        var service = new PlanService(context);
        var result = await service.MarkPlanAsCompletedAsync(plan.Id, 1);

        Assert.True(result.Success);
        var updated = await context.Tasks.FindAsync(plan.Id);
        Assert.True(updated!.IsCompleted);
        Assert.NotNull(updated.CompletedAt);
    }

    [Fact]
    // Нові тести
    public async Task MarkPlanAsCompleted_AlsoCompletesAllSubTasks()
    {
        using var context = CreateContext();

        var plan = new TaskItem { UserId = 1, Title = "План", IsPlan = true, IsCompleted = false };
        context.Tasks.Add(plan);
        await context.SaveChangesAsync();

        var subTask1 = new TaskItem { UserId = 1, Title = "П1", ParentTaskId = plan.Id, IsCompleted = false };
        var subTask2 = new TaskItem { UserId = 1, Title = "П2", ParentTaskId = plan.Id, IsCompleted = false };
        context.Tasks.AddRange(subTask1, subTask2);
        await context.SaveChangesAsync();

        var service = new PlanService(context);
        var result = await service.MarkPlanAsCompletedAsync(plan.Id, 1);

        Assert.True(result.Success);

        var updatedSubTasks = await context.Tasks
            .Where(t => t.ParentTaskId == plan.Id)
            .ToListAsync();

        Assert.NotEmpty(updatedSubTasks);
        Assert.All(updatedSubTasks, t => Assert.True(t.IsCompleted));
        Assert.All(updatedSubTasks, t => Assert.NotNull(t.CompletedAt));
    }

    [Fact]
    public async Task MarkPlanAsIncomplete_SetsFlags()
    {
        using var context = CreateContext();
        var plan = new DOJO2.Domain.Entities.TaskItem { UserId = 1, Title = "p", IsPlan = true, IsCompleted = true, CompletedAt = DateTime.UtcNow };
        context.Tasks.Add(plan);
        await context.SaveChangesAsync();

        var service = new PlanService(context);
        var result = await service.MarkPlanAsIncompleteAsync(plan.Id, 1);

        Assert.True(result.Success);
        var updated = await context.Tasks.FindAsync(plan.Id);
        Assert.False(updated!.IsCompleted);
        Assert.Null(updated.CompletedAt);
    }

    [Fact]
    public async Task DeletePlanAsync_RemovesEntity()
    {
        using var context = CreateContext();
        var plan = new DOJO2.Domain.Entities.TaskItem { UserId = 1, Title = "p", IsPlan = true };
        context.Tasks.Add(plan);
        await context.SaveChangesAsync();

        var service = new PlanService(context);
        var result = await service.DeletePlanAsync(plan.Id, 1);

        Assert.True(result.Success);
        var deleted = await context.Tasks.FindAsync(plan.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task UpdatePlanAsync_ReturnsFailure_WhenModelIsNull()
    {
        var service = BuildPlanServiceWithPlans(new List<TaskItem>(), out _);

        var result = await service.UpdatePlanAsync(1, 1, null);

        Assert.False(result.Success);
        Assert.Contains("не може бути порожною", result.Message);
    }

    [Fact]
    public async Task UpdatePlanAsync_ReturnsFailure_WhenPlanNotFound()
    {
        var service = BuildPlanServiceWithPlans(new List<TaskItem>(), out _);
        var model = new PlanCreateViewModel { Title = "Plan", ScheduledAt = DateTime.UtcNow };

        var result = await service.UpdatePlanAsync(5, 1, model);

        Assert.False(result.Success);
        Assert.Contains("План не знайдено", result.Message);
    }

    [Fact]
    public async Task UpdatePlanAsync_ReturnsFailure_WhenTitleEmpty()
    {
        var plans = new List<TaskItem> { new() { Id = 10, UserId = 3, IsPlan = true, Title = "Old" } };
        var service = BuildPlanServiceWithPlans(plans, out _);
        var model = new PlanCreateViewModel { Title = "   ", ScheduledAt = DateTime.UtcNow };

        var result = await service.UpdatePlanAsync(10, 3, model);

        Assert.False(result.Success);
        Assert.Contains("Назва плану не може бути порожньою", result.Message);
    }

    [Fact]
    public async Task UpdatePlanAsync_ReturnsFailure_WhenTitleTooLong()
    {
        var plans = new List<TaskItem> { new() { Id = 11, UserId = 3, IsPlan = true, Title = "Old" } };
        var service = BuildPlanServiceWithPlans(plans, out _);
        var model = new PlanCreateViewModel { Title = new string('a', 256), ScheduledAt = DateTime.UtcNow };

        var result = await service.UpdatePlanAsync(11, 3, model);

        Assert.False(result.Success);
        Assert.Contains("не може перевищувати 255", result.Message);
    }

    [Fact]
    public async Task UpdatePlanAsync_ReturnsFailure_WhenScheduledAtMissing()
    {
        var plans = new List<TaskItem> { new() { Id = 12, UserId = 3, IsPlan = true, Title = "Old" } };
        var service = BuildPlanServiceWithPlans(plans, out _);
        var model = new PlanCreateViewModel { Title = "Plan", ScheduledAt = null };

        var result = await service.UpdatePlanAsync(12, 3, model);

        Assert.False(result.Success);
        Assert.Contains("Оберіть дату та час плану", result.Message);
    }

    [Fact]
    public async Task UpdatePlanAsync_UpdatesFields_AndSaves()
    {
        var when = new DateTime(2026, 3, 29, 12, 0, 0, DateTimeKind.Utc);
        var plans = new List<TaskItem>
        {
            new()
            {
                Id = 13,
                UserId = 4,
                IsPlan = true,
                Title = "Old",
                Description = "Old desc",
                Priority = 1,
                ScheduledAt = when.AddHours(-1)
            }
        };
        var service = BuildPlanServiceWithPlans(plans, out var contextMock);
        var model = new PlanCreateViewModel
        {
            Title = " New title ",
            Description = "  New desc ",
            Priority = 3,
            ScheduledAt = when
        };

        var result = await service.UpdatePlanAsync(13, 4, model);

        Assert.True(result.Success);
        Assert.Equal("New title", result.Data?.Title);
        Assert.Equal("New desc", result.Data?.Description);
        Assert.Equal((short)3, result.Data?.Priority);
        Assert.Equal(when, result.Data?.ScheduledAt);

        var updated = plans.Single();
        Assert.Equal("New title", updated.Title);
        Assert.Equal("New desc", updated.Description);
        Assert.Equal((short)3, updated.Priority);
        Assert.Equal(when, updated.ScheduledAt);
        contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    // Нові тести
    public async Task CreatePlanSubTaskAsync_ReturnsFailure_WhenPlanNotFound()
    {
        using var context = CreateContext();
        var service = new PlanService(context);

        var result = await service.CreatePlanSubTaskAsync(
            100,
            1,
            new PlanSubTaskCreateViewModel { Title = "Підзадача" }
        );

        Assert.False(result.Success);
        Assert.Contains("План не знайдено", result.Message);
    }

    [Fact]
    public async Task CreatePlanSubTaskAsync_Succeeds_AndPersistsSubTask()
    {
        using var context = CreateContext();
        var plan = new TaskItem
        {
            UserId = 1,
            Title = "План",
            IsPlan = true,
            ScheduledAt = DateTime.UtcNow
        };
        context.Tasks.Add(plan);
        await context.SaveChangesAsync();

        var service = new PlanService(context);
        var result = await service.CreatePlanSubTaskAsync(
            plan.Id,
            1,
            new PlanSubTaskCreateViewModel { Title = " Підзадача 1 " }
        );

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("Підзадача 1", result.Data!.Title);

        var saved = await context.Tasks.FirstOrDefaultAsync(t => t.Id == result.Data.Id);
        Assert.NotNull(saved);
        Assert.Equal(plan.Id, saved!.ParentTaskId);
        Assert.False(saved.IsPlan);
        Assert.False(saved.IsCompleted);
    }

    [Fact]
    public async Task GetPlanSubTasksAsync_ReturnsOnlyPlanSubTasks()
    {
        using var context = CreateContext();
        var plan = new TaskItem { UserId = 1, Title = "План", IsPlan = true, ScheduledAt = DateTime.UtcNow };
        var anotherPlan = new TaskItem { UserId = 1, Title = "Інший", IsPlan = true, ScheduledAt = DateTime.UtcNow };
        context.Tasks.AddRange(plan, anotherPlan);
        await context.SaveChangesAsync();

        context.Tasks.AddRange(
            new TaskItem { UserId = 1, Title = "s1", ParentTaskId = plan.Id, IsCompleted = false, CreatedAt = DateTime.UtcNow },
            new TaskItem { UserId = 1, Title = "s2", ParentTaskId = plan.Id, IsCompleted = true, CreatedAt = DateTime.UtcNow },
            new TaskItem { UserId = 1, Title = "skip", ParentTaskId = anotherPlan.Id, IsCompleted = false, CreatedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var service = new PlanService(context);
        var result = await service.GetPlanSubTasksAsync(plan.Id, 1);

        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.Count);
        Assert.All(result.Data, subTask => Assert.Equal(plan.Id, subTask.ParentPlanId));
    }

    [Fact]
    public async Task UpdatePlanSubTaskAsync_UpdatesSubTaskTitle()
    {
        using var context = CreateContext();
        var plan = new TaskItem { UserId = 1, Title = "План", IsPlan = true, ScheduledAt = DateTime.UtcNow };
        context.Tasks.Add(plan);
        await context.SaveChangesAsync();

        var subTask = new TaskItem { UserId = 1, Title = "Стара", ParentTaskId = plan.Id, IsCompleted = false };
        context.Tasks.Add(subTask);
        await context.SaveChangesAsync();

        var service = new PlanService(context);
        var result = await service.UpdatePlanSubTaskAsync(
            plan.Id,
            subTask.Id,
            1,
            new PlanSubTaskCreateViewModel { Title = " Нова назва " }
        );

        Assert.True(result.Success);
        Assert.Equal("Нова назва", result.Data!.Title);

        var updated = await context.Tasks.FindAsync(subTask.Id);
        Assert.Equal("Нова назва", updated!.Title);
    }

    [Fact]
    public async Task ToggleAndDeletePlanSubTaskAsync_UpdatesStatus_AndDeletes()
    {
        using var context = CreateContext();
        var plan = new TaskItem { UserId = 1, Title = "План", IsPlan = true, ScheduledAt = DateTime.UtcNow };
        context.Tasks.Add(plan);
        await context.SaveChangesAsync();

        var subTask = new TaskItem { UserId = 1, Title = "Підзадача", ParentTaskId = plan.Id, IsCompleted = false };
        context.Tasks.Add(subTask);
        await context.SaveChangesAsync();

        var service = new PlanService(context);

        var completeResult = await service.TogglePlanSubTaskStatusAsync(plan.Id, subTask.Id, 1, true);
        Assert.True(completeResult.Success);
        var completed = await context.Tasks.FindAsync(subTask.Id);
        Assert.True(completed!.IsCompleted);
        Assert.NotNull(completed.CompletedAt);

        var deleteResult = await service.DeletePlanSubTaskAsync(plan.Id, subTask.Id, 1);
        Assert.True(deleteResult.Success);
        var deleted = await context.Tasks.FindAsync(subTask.Id);
        Assert.Null(deleted);
    }
}
