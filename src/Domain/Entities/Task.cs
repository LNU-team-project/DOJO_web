namespace DOJO2.Domain.Entities;

public class TaskItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? GoalId { get; set; }
    public int? ParentTaskId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsCompleted { get; set; } = false;
    public DateOnly? DueDate { get; set; }
    public short Priority { get; set; } = 2;
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsPlan { get; set; } = false;
    public DateTime? ScheduledAt { get; set; }
    public bool XpAwarded { get; set; } = false;

    // Navigation properties
    public virtual AppUser User { get; set; } = null!;
    public virtual Goal? Goal { get; set; }
    public virtual TaskItem? ParentTask { get; set; }
    public virtual ICollection<TaskItem> SubTasks { get; set; } = new List<TaskItem>();
    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public virtual ICollection<Pomodoro> Pomodoros { get; set; } = new List<Pomodoro>();
}
