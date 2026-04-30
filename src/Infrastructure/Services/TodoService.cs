using DOJO2.Domain.Entities;
using DOJO2.Infrastructure.Data;
using DOJO2.Application.Common;
using DOJO2.Application.Interfaces;
using DOJO2.Application.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace DOJO2.Infrastructure.Services;
public class TodoService : ITodoService
{
    private const string TodoNotFoundMessage = "TODO не знайдено";

    private static class PriorityLevels
    {
        public const int Low = 1;
        public const int Medium = 2;
        public const int High = 3;
    }

    private readonly IAppDbContext _context;
    private readonly ILogger<TodoService> _logger;

    public TodoService(IAppDbContext context, ILogger<TodoService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<TodoItemViewModel>> CreateTodoAsync(int userId, TodoCreateViewModel? model)
    {
        if (model == null)
        {
            _logger.LogWarning("Запит створення TODO має null модель для користувача {UserId}", userId);
            return Result<TodoItemViewModel>.FailureResult("Модель TODO не може бути порожньою");
        }

        if (string.IsNullOrWhiteSpace(model.Title))
        {
            _logger.LogWarning("Спроба створення TODO з порожнім заголовком для користувача {UserId}", userId);
            return Result<TodoItemViewModel>.FailureResult("Назва TODO не може бути порожною");
        }

        if (model.Title.Length > 255)
        {
            _logger.LogWarning("Назва TODO перевищує максимальну довжину для користувача {UserId}", userId);
            return Result<TodoItemViewModel>.FailureResult("Назва TODO не може перевищувати 255 символів");
        }

        var todo = new TaskItem
        {
            UserId = userId,
            Title = model.Title.Trim(),
            Description = model.Description?.Trim(),
            Priority = model.Priority,
            DueDate = model.DueDate,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tasks.Add(todo);
        await _context.SaveChangesAsync();

        _logger.LogInformation("TODO успішно створено: {TodoId} для користувача {UserId}", todo.Id, userId);

        return Result<TodoItemViewModel>.SuccessResult(
            MapToViewModel(todo),
            "TODO успішно створено"
        );
    }

    public async Task<Result<TodoListViewModel>> GetUserTodosAsync(int userId)
    {
        var todos = await _context.Tasks
            .Where(t => t.UserId == userId && t.GoalId == null && t.ParentTaskId == null && !t.IsPlan)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var incompleteTodos = SortIncompleteTodos(todos);
        var completedTodos = SortCompletedTodos(todos);

        var result = new TodoListViewModel
        {
            IncompleteTodos = incompleteTodos,
            CompletedTodos = completedTodos
        };

        return Result<TodoListViewModel>.SuccessResult(result, "TODO успішно завантажено");
    }

    public async Task<Result<bool>> MarkTodoAsCompletedAsync(int todoId, int userId)
    {
        var todo = await GetUserTodoAsync(todoId, userId);

        if (todo == null)
        {
            _logger.LogWarning("TODO {TodoId} не знайдено для користувача {UserId}", todoId, userId);
            return Result<bool>.FailureResult(TodoNotFoundMessage);
        }

        if (todo.IsCompleted)
        {
            _logger.LogWarning("TODO {TodoId} вже позначено як виконаний", todoId);
            return Result<bool>.FailureResult("TODO вже позначене як виконане");
        }

        todo.IsCompleted = true;
        todo.CompletedAt = DateTime.UtcNow;

        _context.Tasks.Update(todo);
        await _context.SaveChangesAsync();

        _logger.LogInformation("TODO {TodoId} позначено як виконаний для користувача {UserId}", todoId, userId);

        return Result<bool>.SuccessResult(true, "TODO успішно позначено як виконаний");
    }

    public async Task<Result<bool>> MarkTodoAsIncompleteAsync(int todoId, int userId)
    {
        var todo = await GetUserTodoAsync(todoId, userId);

        if (todo == null)
        {
            _logger.LogWarning("TODO {TodoId} не знайдено для користувача {UserId}", todoId, userId);
            return Result<bool>.FailureResult(TodoNotFoundMessage);
        }

        if (!todo.IsCompleted)
        {
            _logger.LogWarning("TODO {TodoId} вже позначено як невиконаний", todoId);
            return Result<bool>.FailureResult("TODO вже позначено як невиконаний");
        }

        todo.IsCompleted = false;
        todo.CompletedAt = null;

        _context.Tasks.Update(todo);
        await _context.SaveChangesAsync();

        _logger.LogInformation("TODO {TodoId} позначено як невиконаний для користувача {UserId}", todoId, userId);

        return Result<bool>.SuccessResult(true, "TODO успішно позначено як невиконаний");
    }

    public async Task<Result<bool>> DeleteTodoAsync(int todoId, int userId)
    {
        var todo = await GetUserTodoAsync(todoId, userId);

        if (todo == null)
        {
            _logger.LogWarning("TODO {TodoId} не знайдено для видалення користувачем {UserId}", todoId, userId);
            return Result<bool>.FailureResult(TodoNotFoundMessage);
        }

        _context.Tasks.Remove(todo);
        await _context.SaveChangesAsync();

        _logger.LogInformation("TODO {TodoId} видалено для користувача {UserId}", todoId, userId);

        return Result<bool>.SuccessResult(true, "TODO успішно видалено");
    }

    public async Task<Result<TodoItemViewModel>> UpdateTodoAsync(int todoId, int userId, UpdateTodoViewModel? model)
    {
        if (model == null)
        {
            _logger.LogWarning("Запит оновлення TODO має null модель для користувача {UserId}", userId);
            return Result<TodoItemViewModel>.FailureResult("Модель TODO не може бути порожньою");
        }

        if (string.IsNullOrWhiteSpace(model.Title))
        {
            _logger.LogWarning("Спроба оновлення TODO з порожнім заголовком для користувача {UserId}", userId);
            return Result<TodoItemViewModel>.FailureResult("Назва TODO не може бути порожною");
        }

        if (model.Title.Length > 255)
        {
            _logger.LogWarning("Назва TODO перевищує максимальну довжину для користувача {UserId}", userId);
            return Result<TodoItemViewModel>.FailureResult("Назва TODO не може перевищувати 255 символів");
        }

        var todo = await GetUserTodoAsync(todoId, userId);

        if (todo == null)
        {
            _logger.LogWarning("TODO {TodoId} не знайдено для користувача {UserId}", todoId, userId);
            return Result<TodoItemViewModel>.FailureResult(TodoNotFoundMessage);
        }

        todo.Title = model.Title.Trim();
        todo.Description = model.Description?.Trim();
        todo.Priority = model.Priority;
        todo.DueDate = model.DueDate;

        _context.Tasks.Update(todo);
        await _context.SaveChangesAsync();

        _logger.LogInformation("TODO успішно оновлено: {TodoId} для користувача {UserId}", todo.Id, userId);

        return Result<TodoItemViewModel>.SuccessResult(
            MapToViewModel(todo),
            "TODO успішно оновлено"
        );
    }

    private async Task<TaskItem?> GetUserTodoAsync(int todoId, int userId)
    {
        return await _context.Tasks.FirstOrDefaultAsync(t => t.Id == todoId && t.UserId == userId);
    }

    private static List<TodoItemViewModel> SortIncompleteTodos(List<TaskItem> todos)
    {
        return todos
            .Where(t => !t.IsCompleted)
            .OrderBy(t => GetPriorityOrder(t.Priority))
            .ThenBy(t => t.DueDate)
            .Select(MapToViewModel)
            .ToList();
    }

    private static List<TodoItemViewModel> SortCompletedTodos(List<TaskItem> todos)
    {
        return todos
            .Where(t => t.IsCompleted)
            .OrderByDescending(t => t.CompletedAt)
            .Select(MapToViewModel)
            .ToList();
    }

    private static int GetPriorityOrder(int priority)
    {
        return priority switch
        {
            PriorityLevels.High => 0,
            PriorityLevels.Medium => 1,
            PriorityLevels.Low => 2,
            _ => 3
        };
    }

    private static TodoItemViewModel MapToViewModel(TaskItem task)
    {
        var priorityLabel = GetPriorityLabel(task.Priority);

        return new TodoItemViewModel
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Priority = task.Priority,
            DueDate = task.DueDate,
            IsCompleted = task.IsCompleted,
            CreatedAt = task.CreatedAt,
            PriorityLabel = priorityLabel
        };
    }

    private static string GetPriorityLabel(int priority)
    {
        return priority switch
        {
            PriorityLevels.Low => "Низький",
            PriorityLevels.Medium => "Середній",
            PriorityLevels.High => "Високий",
            _ => "Невідомо"
        };
    }
}
