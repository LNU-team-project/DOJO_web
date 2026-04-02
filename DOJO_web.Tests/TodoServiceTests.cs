using DOJO2.Infrastructure.Data;
using DOJO2.Infrastructure.Services;
using DOJO2.Presentation.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace DOJO_web.Tests;

public class TodoServiceTests
{
    private const int TestUserId = 1;
    private const int AnotherUserId = 2;

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static ILogger<TodoService> CreateMockLogger()
    {
        return Mock.Of<ILogger<TodoService>>();
    }

    private static TodoService CreateService(AppDbContext context)
    {
        var logger = CreateMockLogger();
        return new TodoService(context, logger);
    }

    private static DOJO2.Domain.Entities.TaskItem CreateTaskItem(int userId, string title, bool isCompleted = false, string? description = null)
    {
        return new DOJO2.Domain.Entities.TaskItem
        {
            UserId = userId,
            Title = title,
            Description = description,
            IsPlan = false,
            IsCompleted = isCompleted,
            CompletedAt = isCompleted ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow
        };
    }

    // CreateTodoAsync Tests
    [Fact]
    public async Task CreateTodoAsync_Succeeds_AndPersistsTodo()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var model = new TodoCreateViewModel
        {
            Title = "Test todo",
            Description = "Desc",
            Priority = 2,
            DueDate = dueDate
        };

        var result = await service.CreateTodoAsync(TestUserId, model);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("Test todo", result.Data!.Title);
        Assert.Equal(2, result.Data.Priority);
        Assert.Equal(dueDate, result.Data.DueDate);

        var saved = await context.Tasks.FirstOrDefaultAsync(t => t.Id == result.Data.Id);
        Assert.NotNull(saved);
        Assert.Equal(TestUserId, saved.UserId);
        Assert.False(saved.IsPlan);
    }

    [Fact]
    public async Task CreateTodoAsync_ReturnsFailure_WhenTitleEmpty()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var model = new TodoCreateViewModel { Title = "   " };

        var result = await service.CreateTodoAsync(TestUserId, model);

        Assert.False(result.Success);
        Assert.Contains("Назва TODO не може бути порожною", result.Message);
    }

    // GetUserTodosAsync Tests
    [Fact]
    public async Task GetUserTodosAsync_ReturnsSeparatedLists()
    {
        using var context = CreateContext();
        context.Tasks.AddRange(
            CreateTaskItem(TestUserId, "active", isCompleted: false),
            CreateTaskItem(TestUserId, "done", isCompleted: true)
        );
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.GetUserTodosAsync(TestUserId);

        Assert.True(result.Success);
        Assert.Single(result.Data!.IncompleteTodos);
        Assert.Single(result.Data.CompletedTodos);
        Assert.Equal("active", result.Data.IncompleteTodos[0].Title);
        Assert.Equal("done", result.Data.CompletedTodos[0].Title);
    }

    // UpdateTodoAsync Tests
    [Fact]
    public async Task UpdateTodoAsync_SucceedsAndUpdatesTodo()
    {
        using var context = CreateContext();
        var todo = CreateTaskItem(TestUserId, "old title", description: "old desc");
        todo.Priority = 1;
        context.Tasks.Add(todo);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        var model = new UpdateTodoViewModel 
        { 
            Title = "new title", 
            Description = "new desc",
            Priority = 3,
            DueDate = dueDate
        };
        var result = await service.UpdateTodoAsync(todo.Id, TestUserId, model);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("new title", result.Data!.Title);
        Assert.Equal("new desc", result.Data.Description);
        Assert.Equal(3, result.Data.Priority);

        var updated = await context.Tasks.FindAsync(todo.Id);
        Assert.NotNull(updated);
        Assert.Equal("new title", updated.Title);
        Assert.Equal("new desc", updated.Description);
        Assert.Equal(3, updated.Priority);
    }

    [Fact]
    public async Task UpdateTodoAsync_ReturnsFailure_WhenTodoNotFound()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var model = new UpdateTodoViewModel { Title = "new title", Priority = 2 };

        var result = await service.UpdateTodoAsync(999, TestUserId, model);

        Assert.False(result.Success);
        Assert.Contains("не знайдено", result.Message);
    }

    [Fact]
    public async Task UpdateTodoAsync_ReturnsFailure_WhenWrongUser()
    {
        using var context = CreateContext();
        var todo = CreateTaskItem(TestUserId, "title");
        context.Tasks.Add(todo);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var model = new UpdateTodoViewModel { Title = "new title", Priority = 1 };
        var result = await service.UpdateTodoAsync(todo.Id, AnotherUserId, model);

        Assert.False(result.Success);
        Assert.Contains("не знайдено", result.Message);
    }

    [Fact]
    public async Task UpdateTodoAsync_AllowsClearingDescription()
    {
        using var context = CreateContext();
        var todo = CreateTaskItem(TestUserId, "title", description: "old desc");
        context.Tasks.Add(todo);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var model = new UpdateTodoViewModel { Title = "title", Description = null };
        var result = await service.UpdateTodoAsync(todo.Id, TestUserId, model);

        Assert.True(result.Success);
        var updated = await context.Tasks.FindAsync(todo.Id);
        Assert.Null(updated!.Description);
    }

    // Mark as Completed Tests
    [Fact]
    public async Task MarkTodoAsCompleted_SetsFlags()
    {
        using var context = CreateContext();
        var todo = CreateTaskItem(TestUserId, "title");
        context.Tasks.Add(todo);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.MarkTodoAsCompletedAsync(todo.Id, TestUserId);

        Assert.True(result.Success);
        var updated = await context.Tasks.FindAsync(todo.Id);
        Assert.True(updated!.IsCompleted);
        Assert.NotNull(updated.CompletedAt);
    }

    [Fact]
    public async Task MarkTodoAsCompleted_ReturnsFailure_WhenAlreadyCompleted()
    {
        using var context = CreateContext();
        var todo = CreateTaskItem(TestUserId, "title", isCompleted: true);
        context.Tasks.Add(todo);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.MarkTodoAsCompletedAsync(todo.Id, TestUserId);

        Assert.False(result.Success);
        Assert.Contains("вже позначене як виконане", result.Message);
    }

    // Mark as Incomplete Tests
    [Fact]
    public async Task MarkTodoAsIncomplete_SetsFlags()
    {
        using var context = CreateContext();
        var todo = CreateTaskItem(TestUserId, "title", isCompleted: true);
        context.Tasks.Add(todo);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.MarkTodoAsIncompleteAsync(todo.Id, TestUserId);

        Assert.True(result.Success);
        var updated = await context.Tasks.FindAsync(todo.Id);
        Assert.False(updated!.IsCompleted);
        Assert.Null(updated.CompletedAt);
    }

    // Delete Tests
    [Fact]
    public async Task DeleteTodoAsync_RemovesEntity()
    {
        using var context = CreateContext();
        var todo = CreateTaskItem(TestUserId, "title");
        context.Tasks.Add(todo);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.DeleteTodoAsync(todo.Id, TestUserId);

        Assert.True(result.Success);
        var deleted = await context.Tasks.FindAsync(todo.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteTodoAsync_ReturnsFailure_WhenTodoNotFound()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.DeleteTodoAsync(999, TestUserId);

        Assert.False(result.Success);
        Assert.Contains("не знайдено", result.Message);
    }
}
