using DOJO2.Application.Common;
using DOJO2.Application.Interfaces;
using DOJO2.Application.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DOJO2.Controllers;

[Authorize]
[Route("api/rooms")]
public class RoomsController : BaseApiController
{
    private readonly IRoomService _roomService;

    public RoomsController(IRoomService roomService, ILogger<RoomsController> logger)
        : base(logger)
    {
        _roomService = roomService ?? throw new ArgumentNullException(nameof(roomService));
    }

    [HttpGet]
    public async Task<IActionResult> GetMyRooms()
    {
        var authError = ValidateUserAuthorization();
        if (authError != null) return authError;

        var userId = GetCurrentUserId() ?? 0;
        var result = await _roomService.GetMyRoomsAsync(userId);
        return ToActionResult(result);
    }

    [HttpGet("{roomId}")]
    public async Task<IActionResult> GetRoom(int roomId)
    {
        var authError = ValidateUserAuthorization();
        if (authError != null) return authError;

        var userId = GetCurrentUserId() ?? 0;
        var result = await _roomService.GetRoomAsync(userId, roomId);
        return ToActionResult(result);
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest? request)
    {
        var authError = ValidateUserAuthorization();
        if (authError != null) return authError;

        if (request == null)
        {
            return BadRequest(new { success = false, message = "Невірні дані для створення кімнати" });
        }

        var userId = GetCurrentUserId() ?? 0;
        var result = await _roomService.CreateRoomAsync(userId, request);
        return ToActionResult(result);
    }

    [HttpPost("{roomId}/members/add")]
    public async Task<IActionResult> AddMember(int roomId, [FromBody] AddRoomMemberRequest? request)
    {
        var authError = ValidateUserAuthorization();
        if (authError != null) return authError;

        if (request == null || request.UserId <= 0)
        {
            return BadRequest(new { success = false, message = "Невірні дані" });
        }

        var userId = GetCurrentUserId() ?? 0;
        var result = await _roomService.AddMemberAsync(userId, roomId, request.UserId);
        return ToActionResult(result);
    }

    [HttpDelete("{roomId}/members/{memberId}")]
    public async Task<IActionResult> RemoveMember(int roomId, int memberId)
    {
        var authError = ValidateUserAuthorization();
        if (authError != null) return authError;

        var userId = GetCurrentUserId() ?? 0;
        var result = await _roomService.RemoveMemberAsync(userId, roomId, memberId);
        return ToActionResult(result);
    }

    [HttpPost("{roomId}/tasks")]
    public async Task<IActionResult> CreateTask(int roomId, [FromBody] CreateRoomTaskRequest? request)
    {
        var authError = ValidateUserAuthorization();
        if (authError != null) return authError;

        if (request == null)
        {
            return BadRequest(new { success = false, message = "Невірні дані для створення завдання" });
        }

        var userId = GetCurrentUserId() ?? 0;
        var result = await _roomService.CreateTaskAsync(userId, roomId, request);
        return ToActionResult(result);
    }

    [HttpPost("tasks/{taskId}/comments")]
    public async Task<IActionResult> AddComment(int taskId, [FromBody] AddRoomTaskCommentRequest? request)
    {
        var authError = ValidateUserAuthorization();
        if (authError != null) return authError;

        if (request == null || string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new { success = false, message = "Текст коментара не може бути порожним" });
        }

        var userId = GetCurrentUserId() ?? 0;
        var result = await _roomService.AddCommentAsync(userId, taskId, request.Text);
        return ToActionResult(result);
    }
}

public class AddRoomMemberRequest
{
    public int UserId { get; set; }
}

public class AddRoomTaskCommentRequest
{
    public string Text { get; set; } = string.Empty;
}

