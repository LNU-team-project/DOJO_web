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
}

