using DOJO2.Infrastructure.Services;
using DOJO2.Presentation.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DOJO2.Controllers;


[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TodoController : ControllerBase
{
    private readonly ITodoService _todoService;
    private readonly ILogger<TodoController> _logger;

    public TodoController(ITodoService todoService, ILogger<TodoController> logger)
    {
        _todoService = todoService ?? throw new ArgumentNullException(nameof(todoService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    
    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId)
            ? userId
            : null;
    }

    /// <summary>
    /// Перевіряє авторизацію користувача
    /// </summary>
    private IActionResult? ValidateUserAuthorization()
    {
        var userId = GetCurrentUserId();
        if (userId == null || userId <= 0)
        {
            _logger.LogWarning("Невалідний userId або користувач не авторизований");
            return Unauthorized(new { success = false, message = "Користувача не знайдено" });
        }

        return null;
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

        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message, errors = result.Errors });
        }

        return Ok(new { success = true, message = result.Message, data = result.Data });
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

        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message, errors = result.Errors });
        }

        return Ok(new { success = true, message = result.Message, data = result.Data });
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
            return BadRequest(new { success = false, message = result.Message, errors = result.Errors });
        }

        return Ok(new { success = true, message = result.Message });
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

        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message, errors = result.Errors });
        }

        return Ok(new { success = true, message = result.Message });
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

        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message, errors = result.Errors });
        }

        return Ok(new { success = true, message = result.Message });
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

        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message, errors = result.Errors });
        }

        return Ok(new { success = true, message = result.Message, data = result.Data });
    }
}
