namespace DOJO2.Domain.Entities;

public class ScheduleItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartAt { get; set; }
    public short DurationMinutes { get; set; } = 60;
    public short Priority { get; set; } = 2;
    public string RecurrenceType { get; set; } = "none";
    public short RecurrenceInterval { get; set; } = 1;
    public short WeeklyDaysMask { get; set; } = 0;
    public DateOnly? RecurrenceEndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual AppUser User { get; set; } = null!;
}
