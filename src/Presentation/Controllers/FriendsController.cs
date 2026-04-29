using DOJO2.Application.Common;
using DOJO2.Application.Interfaces;
using DOJO2.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DOJO2.Controllers;

[Authorize]
public class FriendsController : BaseApiController
{
    private readonly IUserService _userService;

    public FriendsController(IUserService userService, ILogger<FriendsController> logger)
        : base(logger)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
    }

    [HttpGet]
    public async Task<IActionResult> GetMyFriends()
    {
        var authError = ValidateUserAuthorization();
        if (authError != null) return authError;

        var userId = GetCurrentUserId() ?? 0;
        var result = await _userService.GetFriendsAsync(userId);
        return ToActionResult(result);
    }

    [HttpPost("add")]
    public async Task<IActionResult> AddFriend([FromBody] AddFriendRequest? request)
    {
        var authError = ValidateUserAuthorization();
        if (authError != null) return authError;

        if (request == null)
        {
            return BadRequest(new { success = false, message = "Невірні дані для додавання друга" });
        }

        var userId = GetCurrentUserId() ?? 0;
        Result<bool> result;

        if (!string.IsNullOrWhiteSpace(request.FriendUserName))
        {
            result = await _userService.SendFriendRequestAsync(userId, request.FriendUserName);
        }
        else
        {
            return BadRequest(new { success = false, message = "Вкажіть ім'я користувача" });
        }

        return ToActionResult(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchUsers([FromQuery] string? query, [FromQuery] int limit = 5)
    {
        var authError = ValidateUserAuthorization();
        if (authError != null) return authError;

        var userId = GetCurrentUserId() ?? 0;
        var result = await _userService.SearchUsersAsync(userId, query ?? string.Empty, limit);
        return ToActionResult(result);
    }

    [HttpGet("requests")]
    public async Task<IActionResult> GetIncomingRequests()
    {
        var authError = ValidateUserAuthorization();
        if (authError != null) return authError;

        var userId = GetCurrentUserId() ?? 0;
        var result = await _userService.GetIncomingFriendRequestsAsync(userId);
        return ToActionResult(result);
    }

    [HttpPost("requests/{requestId}/accept")]
    public async Task<IActionResult> AcceptRequest(int requestId)
    {
        var authError = ValidateUserAuthorization();
        if (authError != null) return authError;

        var userId = GetCurrentUserId() ?? 0;
        var result = await _userService.AcceptFriendRequestAsync(userId, requestId);
        return ToActionResult(result);
    }

    [HttpPost("requests/{requestId}/decline")]
    public async Task<IActionResult> DeclineRequest(int requestId)
    {
        var authError = ValidateUserAuthorization();
        if (authError != null) return authError;

        var userId = GetCurrentUserId() ?? 0;
        var result = await _userService.DeclineFriendRequestAsync(userId, requestId);
        return ToActionResult(result);
    }

    [HttpDelete("remove/{friendUserId}")]
    public async Task<IActionResult> RemoveFriend(int friendUserId)
    {
        var authError = ValidateUserAuthorization();
        if (authError != null) return authError;

        var userId = GetCurrentUserId() ?? 0;
        var result = await _userService.RemoveFriendAsync(userId, friendUserId);
        return ToActionResult(result);
    }
}
