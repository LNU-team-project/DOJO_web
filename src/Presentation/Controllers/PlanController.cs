using DOJO2.Infrastructure.Services;
using DOJO2.Presentation.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DOJO2.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlanController : ControllerBase
{
    private readonly IPlanService _planService;
    private readonly ILogger<PlanController> _logger;

    public PlanController(IPlanService planService, ILogger<PlanController> logger)
    {
        _planService = planService ?? throw new ArgumentNullException(nameof(planService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId)
            ? userId
            : null;
    }

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
    public async Task<IActionResult> CreatePlan([FromBody] PlanCreateViewModel? model)
    {
        if (model == null)
        {
            return BadRequest(new { success = false, message = "Модель плану не може бути порожньою" });
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
        var result = await _planService.CreatePlanAsync(userId, model);

        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message, errors = result.Errors });
        }

        return Ok(new { success = true, message = result.Message, data = result.Data });
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetPlans()
    {
        var authError = ValidateUserAuthorization();
        if (authError != null)
        {
            return authError;
        }

        var userId = GetCurrentUserId() ?? 0;
        var result = await _planService.GetUserPlansAsync(userId);

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
        var result = await _planService.MarkPlanAsCompletedAsync(id, userId);

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
        var result = await _planService.MarkPlanAsIncompleteAsync(id, userId);

        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message, errors = result.Errors });
        }

        return Ok(new { success = true, message = result.Message });
    }

    [HttpDelete("delete/{id:int}")]
    public async Task<IActionResult> DeletePlan(int id)
    {
        var authError = ValidateUserAuthorization();
        if (authError != null)
        {
            return authError;
        }

        var userId = GetCurrentUserId() ?? 0;
        var result = await _planService.DeletePlanAsync(id, userId);

        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message, errors = result.Errors });
        }

        return Ok(new { success = true, message = result.Message });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetPlan(int id)
    {
        var authError = ValidateUserAuthorization();
        if (authError != null) return authError;

        var userId = GetCurrentUserId() ?? 0;
        var result = await _planService.GetPlanByIdAsync(id, userId);
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message, errors = result.Errors });
        }
        return Ok(new { success = true, message = result.Message, data = result.Data });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdatePlan(int id, [FromBody] PlanCreateViewModel? model)
    {
        if (model == null) return BadRequest(new { success = false, message = "Модель плану не може бути порожньою" });
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new { success = false, message = "Невалідні дані", errors });
        }

        var authError = ValidateUserAuthorization();
        if (authError != null) return authError;

        var userId = GetCurrentUserId() ?? 0;
        var result = await _planService.UpdatePlanAsync(id, userId, model);
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message, errors = result.Errors });
        }

        return Ok(new { success = true, message = result.Message, data = result.Data });
    }
}
