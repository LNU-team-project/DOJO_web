using System;

namespace DOJO2.Domain.Entities;

public class RoomTaskComment
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int AuthorUserId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Navigation
    public RoomTask? Task { get; set; }
    public AppUser? AuthorUser { get; set; }
}

