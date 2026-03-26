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
        return new Mock<ILogger<TodoService>>().Object;
    }

    private static TodoService CreateService(AppDbContext context)
    {
        var logger = CreateMockLogger();
        return new TodoService(context, logger);
    }

    [Fact]
    public async Task CreateTodoAsync_ReturnsFailure_WhenModelIsNull()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.CreateTodoAsync(TestUserId, null);

        Assert.False(result.Success);
        Assert.Contains("не може бути порожною", result.Message);
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

    [Fact]
    public async Task CreateTodoAsync_ReturnsFailure_WhenTitleTooLong()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var model = new TodoCreateViewModel { Title = new string('a', 256) };

        var result = await service.CreateTodoAsync(TestUserId, model);

        Assert.False(result.Success);
        Assert.Contains("Назва TODO не може перевищувати 255 символів", result.Message);
    }

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
    public async Task GetUserTodosAsync_ReturnsSeparatedLists()
    {
        using var context = CreateContext();
        context.Tasks.AddRange(
            new DOJO2.Domain.Entities.TaskItem 
            { 
                UserId = TestUserId, 
                Title = "active", 
                IsPlan = false, 
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow
            },
            new DOJO2.Domain.Entities.TaskItem 
            { 
                UserId = TestUserId, 
                Title = "done", 
                IsPlan = false, 
                IsCompleted = true, 
                CompletedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
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

    [Fact]
    public async Task MarkTodoAsCompleted_SetsFlags()
    {
        using var context = CreateContext();
        var todo = new DOJO2.Domain.Entities.TaskItem 
        { 
            UserId = TestUserId, 
            Title = "t", 
            IsPlan = false, 
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };
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
    public async Task MarkTodoAsCompleted_ReturnsFailure_WhenTodoNotFound()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.MarkTodoAsCompletedAsync(999, TestUserId);

        Assert.False(result.Success);
        Assert.Contains("не знайдено", result.Message);
    }

    [Fact]
    public async Task MarkTodoAsCompleted_ReturnsFailure_WhenWrongUser()
    {
        using var context = CreateContext();
        var todo = new DOJO2.Domain.Entities.TaskItem 
        { 
            UserId = TestUserId, 
            Title = "t", 
            IsPlan = false, 
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };
        context.Tasks.Add(todo);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.MarkTodoAsCompletedAsync(todo.Id, AnotherUserId);

        Assert.False(result.Success);
        Assert.Contains("не знайдено", result.Message);
    }

    [Fact]
    public async Task MarkTodoAsIncomplete_SetsFlags()
    {
        using var context = CreateContext();
        var todo = new DOJO2.Domain.Entities.TaskItem 
        { 
            UserId = TestUserId, 
            Title = "t", 
            IsPlan = false, 
            IsCompleted = true, 
            CompletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        context.Tasks.Add(todo);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.MarkTodoAsIncompleteAsync(todo.Id, TestUserId);

        Assert.True(result.Success);
        var updated = await context.Tasks.FindAsync(todo.Id);
        Assert.False(updated!.IsCompleted);
        Assert.Null(updated.CompletedAt);
    }

    [Fact]
    public async Task MarkTodoAsIncomplete_ReturnsFailure_WhenTodoNotFound()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.MarkTodoAsIncompleteAsync(999, TestUserId);

        Assert.False(result.Success);
        Assert.Contains("не знайдено", result.Message);
    }

    [Fact]
    public async Task DeleteTodoAsync_RemovesEntity()
    {
        using var context = CreateContext();
        var todo = new DOJO2.Domain.Entities.TaskItem 
        { 
            UserId = TestUserId, 
            Title = "t", 
            IsPlan = false,
            CreatedAt = DateTime.UtcNow
        };
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
