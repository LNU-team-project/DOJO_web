using DOJO2.Infrastructure.Services;
using DOJO2.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DOJO2.Controllers;

[Authorize]
public class HeroController : BaseApiController
{
    private readonly IHeroService _heroService;

    public HeroController(IHeroService heroService, ILogger<HeroController> logger) : base(logger)
    {
        _heroService = heroService ?? throw new ArgumentNullException(nameof(heroService));
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var authError = ValidateUserAuthorization();
        if (authError != null) return authError;

        var userId = GetCurrentUserId() ?? 0;
        var result = await _heroService.GetHeroStatusAsync(userId);
        return ToActionResult(result);
    }

    [HttpPost("award/task/{taskId:int}")]
    public async Task<IActionResult> AwardForTask(int taskId)
    {
        var authError = ValidateUserAuthorization();
        if (authError != null) return authError;

        var userId = GetCurrentUserId() ?? 0;
        var result = await _heroService.AwardExpForTaskAsync(taskId, userId);
        return ToActionResult(result);
    }
}

