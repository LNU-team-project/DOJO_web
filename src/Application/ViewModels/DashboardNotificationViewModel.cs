namespace DOJO2.Application.ViewModels;

public enum NotificationSeverity
{
    Info = 1,
    Warning = 2
}

public class DashboardNotificationViewModel
{
    public NotificationSeverity Severity { get; set; }
    public string Badge { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<NotificationActionViewModel> Actions { get; set; } = new();
}

public class NotificationActionViewModel
{
    public string Label { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public int RequestId { get; set; }
}

