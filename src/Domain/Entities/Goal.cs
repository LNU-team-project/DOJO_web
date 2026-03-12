namespace DOJO2.Domain.Entities;

public class Goal
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public DateOnly? Deadline { get; set; }
    public short Priority { get; set; } = 2;
    public short Progress { get; set; } = 0;
    public bool IsCompleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}

