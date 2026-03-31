using System.ComponentModel.DataAnnotations;

namespace DOJO2.Presentation.ViewModels;

public class PomodoroSessionCreateViewModel
{
    [Range(1, 180)]
    public short DurationMinutes { get; set; }

    [Range(1, 100)]
    public short WorkCycles { get; set; } = 1;

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    public int? TaskId { get; set; }
}

public class PomodoroTodayStatsViewModel
{
    public int CompletedFocusSessions { get; set; }
    public int TotalFocusMinutes { get; set; }
}
