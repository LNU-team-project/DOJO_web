namespace DOJO2.Domain.Entities;

public class Pomodoro
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? TaskId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public short? DurationMinutes { get; set; }
    public short WorkCycles { get; set; } = 1;

    // Navigation properties
    public User User { get; set; } = null!;
    public TaskItem? Task { get; set; }
}

