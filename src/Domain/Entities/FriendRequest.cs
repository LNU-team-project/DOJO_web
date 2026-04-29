using System;

namespace DOJO2.Domain.Entities;

public class FriendRequest
{
    public int Id { get; set; }

    public int RequesterUserId { get; set; }
    public AppUser? RequesterUser { get; set; }

    public int ReceiverUserId { get; set; }
    public AppUser? ReceiverUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
