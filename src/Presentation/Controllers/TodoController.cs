using DOJO2.Infrastructure.Services;
using DOJO2.Presentation.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DOJO2.Controllers;

[Authorize]
public class TodoController : BaseApiController
{
    private readonly ITodoService _todoService;
    private readonly IHeroService _heroService;

    public TodoController(ITodoService todoService, IHeroService heroService, ILogger<TodoController> logger)
        : base(logger)
    {
        _todoService = todoService ?? throw new ArgumentNullException(nameof(todoService));
        _heroService = heroService ?? throw new ArgumentNullException(nameof(heroService));
    }

    
    [HttpPost("create")]
    public async Task<IActionResult> CreateTodo([FromBody] TodoCreateViewModel? model)
    {
        if (model == null)
        {
            return BadRequest(new { success = false, message = "Модель завдання не може бути порожною" });
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new { success = false, message = "Невалідні дані", errors });
        }

        var authError = ValidateUserAuthorization();
        if (authError != null)
        {
            return authError;
        }

        var userId = GetCurrentUserId() ?? 0;
        var result = await _todoService.CreateTodoAsync(userId, model);

        return ToActionResult(result);
    }

    
    [HttpGet("list")]
    public async Task<IActionResult> GetTodos()
    {
        var authError = ValidateUserAuthorization();
        if (authError != null)
        {
            return authError;
        }

        var userId = GetCurrentUserId() ?? 0;
        var result = await _todoService.GetUserTodosAsync(userId);

        return ToActionResult(result);
    }

    
    [HttpPut("complete/{id:int}")]
    public async Task<IActionResult> MarkAsCompleted(int id)
    {
        var authError = ValidateUserAuthorization();
        if (authError != null)
        {
            return authError;
        }

        var userId = GetCurrentUserId() ?? 0;
        var result = await _todoService.MarkTodoAsCompletedAsync(id, userId);

        if (!result.Success)
        {
            return ToActionResult(result);
        }

        // Після успішного виконання туду — нарахувати XP та повернути оновлений статус героя
        var heroResult = await _heroService.AwardExpForTaskAsync(id, userId);
        return ToActionResult(heroResult);
    }

  
    [HttpPut("incomplete/{id:int}")]
    public async Task<IActionResult> MarkAsIncomplete(int id)
    {
        var authError = ValidateUserAuthorization();
        if (authError != null)
        {
            return authError;
        }

        var userId = GetCurrentUserId() ?? 0;
        var result = await _todoService.MarkTodoAsIncompleteAsync(id, userId);

        return ToActionResult(result);
    }

    
    [HttpDelete("delete/{id:int}")]
    public async Task<IActionResult> DeleteTodo(int id)
    {
        var authError = ValidateUserAuthorization();
        if (authError != null)
        {
            return authError;
        }

        var userId = GetCurrentUserId() ?? 0;
        var result = await _todoService.DeleteTodoAsync(id, userId);

        return ToActionResult(result);
    }

  
    [HttpPut("update/{id:int}")]
    public async Task<IActionResult> UpdateTodo(int id, [FromBody] UpdateTodoViewModel? model)
    {
        if (model == null)
        {
            return BadRequest(new { success = false, message = "Модель завдання не може бути порожною" });
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new { success = false, message = "Невалідні дані", errors });
        }

        var authError = ValidateUserAuthorization();
        if (authError != null)
        {
            return authError;
        }

        var userId = GetCurrentUserId() ?? 0;
        var result = await _todoService.UpdateTodoAsync(id, userId, model);

        return ToActionResult(result);
    }
}
