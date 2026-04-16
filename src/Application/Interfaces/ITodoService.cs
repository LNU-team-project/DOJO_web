using DOJO2.Application.ViewModels;
using DOJO2.Infrastructure.Results;

namespace DOJO2.Application.Interfaces;

public interface ITodoService
{
    Task<Result<TodoItemViewModel>> CreateTodoAsync(int userId, TodoCreateViewModel model);
    Task<Result<TodoListViewModel>> GetUserTodosAsync(int userId);
    Task<Result<bool>> MarkTodoAsCompletedAsync(int todoId, int userId);
    Task<Result<bool>> MarkTodoAsIncompleteAsync(int todoId, int userId);
    Task<Result<bool>> DeleteTodoAsync(int todoId, int userId);
    Task<Result<TodoItemViewModel>> UpdateTodoAsync(int todoId, int userId, UpdateTodoViewModel? model);
}
