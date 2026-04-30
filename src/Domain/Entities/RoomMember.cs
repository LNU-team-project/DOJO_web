using System;

namespace DOJO2.Domain.Entities;

public class RoomMember
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public int UserId { get; set; }
    public DateTime JoinedAt { get; set; }

    // Navigation
    public Room? Room { get; set; }
    public AppUser? User { get; set; }
}

