using DOJO2.Application.Common;
using DOJO2.Application.ViewModels;

namespace DOJO2.Application.Interfaces;

public interface IRoomService
{
    Task<Result<List<RoomViewModel>>> GetMyRoomsAsync(int userId);
    Task<Result<RoomViewModel>> GetRoomAsync(int userId, int roomId);
    Task<Result<int>> CreateRoomAsync(int userId, CreateRoomRequest model);
    Task<Result<bool>> AddMemberAsync(int userId, int roomId, int memberUserId);
    Task<Result<bool>> RemoveMemberAsync(int userId, int roomId, int memberUserId);
    Task<Result<int>> CreateTaskAsync(int userId, int roomId, CreateRoomTaskRequest request);
    Task<Result<bool>> AddCommentAsync(int userId, int taskId, string text);
}

