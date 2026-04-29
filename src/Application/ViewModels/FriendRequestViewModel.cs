namespace DOJO2.Application.ViewModels;

public class FriendRequestViewModel
{
    public int RequestId { get; set; }
    public int RequesterUserId { get; set; }
    public string RequesterUserName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
