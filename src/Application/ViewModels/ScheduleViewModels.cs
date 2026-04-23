namespace DOJO2.Application.ViewModels;

public class ScheduleCreateViewModel
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? StartAt { get; set; }
    public short DurationMinutes { get; set; } = 60;
    public short Priority { get; set; } = 2;
    public string RecurrenceType { get; set; } = "none";
    public short RecurrenceInterval { get; set; } = 1;
    public List<int> WeeklyDays { get; set; } = new();
    public DateOnly? RecurrenceEndDate { get; set; }
}

public class ScheduleItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartAt { get; set; }
    public short DurationMinutes { get; set; }
    public short Priority { get; set; }
    public string PriorityLabel { get; set; } = string.Empty;
    public string RecurrenceType { get; set; } = "none";
    public short RecurrenceInterval { get; set; }
    public List<int> WeeklyDays { get; set; } = new();
    public DateOnly? RecurrenceEndDate { get; set; }
}

public class ScheduleOccurrenceViewModel
{
    public int ScheduleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime OccurrenceAt { get; set; }
    public short DurationMinutes { get; set; }
    public short Priority { get; set; }
    public string PriorityLabel { get; set; } = string.Empty;
    public string RecurrenceType { get; set; } = "none";
    public short RecurrenceInterval { get; set; }
    public DateOnly? RecurrenceEndDate { get; set; }
    public List<int> WeeklyDays { get; set; } = new();
}

public class ScheduleDeleteViewModel
{
    public int ScheduleId { get; set; }
    public DateTime? OccurrenceAt { get; set; }
    public string DeleteMode { get; set; } = "single";
}
