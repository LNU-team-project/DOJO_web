using System;
using System.Collections.Generic;

namespace DOJO2.Domain.Entities;

public class RoomTask
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public int AssignedToUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Room? Room { get; set; }
    public AppUser? AssignedToUser { get; set; }
    public List<RoomTaskComment> Comments { get; set; } = new();
}

