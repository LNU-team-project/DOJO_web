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

    // Navigation properties
    public User User { get; set; } = null!;
    public Goal? Goal { get; set; }
    public TaskItem? ParentTask { get; set; }
    public ICollection<TaskItem> SubTasks { get; set; } = new List<TaskItem>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public ICollection<Pomodoro> Pomodoros { get; set; } = new List<Pomodoro>();
}

