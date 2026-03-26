using Microsoft.AspNetCore.Identity;

namespace DOJO2.Domain.Entities;

public class AppUser : IdentityUser<int>
{
    public int ExpPoints { get; set; } = 0;
    public int Level { get; set; } = 1;
    public int CurrentStreak { get; set; } = 0;
    public DateOnly? LastCompletionDate { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Goal> Goals { get; set; } = new List<Goal>();
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public ICollection<Pomodoro> Pomodoros { get; set; } = new List<Pomodoro>();
}