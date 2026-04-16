using DOJO2.Application.Interfaces;
using DOJO2.Application.Common;
using DOJO2.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DOJO2.Controllers;

[Authorize]
public class PlanController : BaseApiController
{
    private readonly IPlanService _planService;
    private readonly IHeroService _heroService;

    public PlanController(IPlanService planService, IHeroService heroService, ILogger<PlanController> logger)
        : base(logger)
    {
        _planService = planService ?? throw new ArgumentNullException(nameof(planService));
        _heroService = heroService ?? throw new ArgumentNullException(nameof(heroService));
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

        if (!result.Success)
        {
            return ToActionResult(result);
        }

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

    [HttpGet("{id:int}/attachments")]
    public async Task<IActionResult> GetPlanAttachments(int id)
    {
        var authError = ValidateUserAuthorization();
        if (authError != null)
        {
            return authError;
        }

        var userId = GetCurrentUserId() ?? 0;
        var result = await _planService.GetPlanAttachmentsAsync(id, userId);
        return ToActionResult(result);
    }

    [HttpPost("{id:int}/attachments")]
    public async Task<IActionResult> UploadPlanAttachment(int id, [FromForm] IFormFile? file)
    {
        var authError = ValidateUserAuthorization();
        if (authError != null)
        {
            return authError;
        }

        var userId = GetCurrentUserId() ?? 0;
        var uploadData = await BuildUploadDataAsync(file);
        var result = await _planService.UploadPlanAttachmentAsync(id, userId, uploadData);
        return ToActionResult(result);
    }

    [HttpDelete("{id:int}/attachments/{attachmentId:int}")]
    public async Task<IActionResult> DeletePlanAttachment(int id, int attachmentId)
    {
        var authError = ValidateUserAuthorization();
        if (authError != null)
        {
            return authError;
        }

        var userId = GetCurrentUserId() ?? 0;
        var result = await _planService.DeletePlanAttachmentAsync(id, attachmentId, userId);
        return ToActionResult(result);
    }

    private static async Task<FileUploadData?> BuildUploadDataAsync(IFormFile? file)
    {
        if (file == null)
        {
            return null;
        }

        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms);

        return new FileUploadData
        {
            FileName = file.FileName,
            ContentType = file.ContentType,
            Length = file.Length,
            Content = ms.ToArray()
        };
    }
}
