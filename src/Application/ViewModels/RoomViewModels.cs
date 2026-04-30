using System;
using System.Collections.Generic;

namespace DOJO2.Application.ViewModels
{
    public class RoomViewModel
    {
        public int Id { get; set; }
        public int OwnerUserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<RoomMemberViewModel> Members { get; set; } = new();
        public List<RoomTaskViewModel> Tasks { get; set; } = new();
    }

    public class RoomMemberViewModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public DateTime JoinedAt { get; set; }
    }

    public class RoomTaskViewModel
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public int AssignedToUserId { get; set; }
        public string AssignedToUserName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<RoomTaskCommentViewModel> Comments { get; set; } = new();
    }

    public class RoomTaskCommentViewModel
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public int AuthorUserId { get; set; }
        public string AuthorUserName { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateRoomRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<int> MemberUserIds { get; set; } = new();
    }

    public class CreateRoomTaskRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int AssignedToUserId { get; set; }
        public DateTime? DueDate { get; set; }
    }
}

