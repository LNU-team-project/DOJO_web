using System;
using System.Collections.Generic;

namespace DOJO2.Domain.Entities;

public class Room
{
    public int Id { get; set; }
    public int OwnerUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public AppUser? OwnerUser { get; set; }
    public List<RoomMember> Members { get; set; } = new();
    public List<RoomTask> Tasks { get; set; } = new();
}

