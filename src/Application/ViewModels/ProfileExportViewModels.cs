namespace DOJO2.Application.ViewModels;

public class ProfileExportRequestViewModel
{
    public bool IncludeLevel { get; set; } = true;
    public bool IncludeExpPoints { get; set; } = true;
    public bool IncludeCurrentStreak { get; set; } = true;
    public bool IncludeCompletedPlans { get; set; } = true;
    public bool IncludeCompletedTasks { get; set; } = true;
    public bool IncludePomodoroSessions { get; set; } = true;
    public bool IncludeFocusMinutes { get; set; } = true;

    public bool HasSelectedFields()
    {
        return IncludeLevel
            || IncludeExpPoints
            || IncludeCurrentStreak
            || IncludeCompletedPlans
            || IncludeCompletedTasks
            || IncludePomodoroSessions
            || IncludeFocusMinutes;
    }
}

public class ProfileExportFileViewModel
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text/csv; charset=utf-8";
    public byte[] Content { get; set; } = Array.Empty<byte>();
}

