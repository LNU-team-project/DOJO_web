using System;

namespace DOJO2.Domain.Entities;

public class Friend
{
    public int Id { get; set; }

    // The owner of this friend entry (the user who added the friend)
    public int UserId { get; set; }
    public AppUser? User { get; set; }

    // The referenced friend user
    public int FriendUserId { get; set; }
    public AppUser? FriendUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
