namespace DOJO2.Application.ViewModels;

public class FriendViewModel
{
    public int Id { get; set; }
    public int FriendUserId { get; set; }
    public string FriendUserName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
