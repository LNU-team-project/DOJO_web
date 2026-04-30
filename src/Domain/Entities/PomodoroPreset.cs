namespace DOJO2.Domain.Entities;

public class PomodoroPreset
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public short FocusMinutes { get; set; }
    public short ShortBreakMinutes { get; set; }
    public short LongBreakMinutes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AppUser? User { get; set; }
}