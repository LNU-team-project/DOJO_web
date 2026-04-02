using DOJO2.Infrastructure.Services;
using DOJO2.Presentation.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DOJO2.Controllers;

[Authorize]
public class PlanController : BaseApiController
{
    private readonly IPlanService _planService;

    public PlanController(IPlanService planService, ILogger<PlanController> logger)
        : base(logger)
    {
        _planService = planService ?? throw new ArgumentNullException(nameof(planService));
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

        return ToActionResult(result);
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
        var result = await _planService.MarkPlanAsCompletedAsync(id, userId);

        return ToActionResult(result);
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

        return ToActionResult(result);
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

        return ToActionResult(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetPlan(int id)
    {
        var authError = ValidateUserAuthorization();
        if (authError != null) return authError;

        var userId = GetCurrentUserId() ?? 0;
        var result = await _planService.GetPlanByIdAsync(id, userId);
        return ToActionResult(result);
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
        return ToActionResult(result);
    }
}
