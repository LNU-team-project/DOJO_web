namespace DOJO2.Domain.Entities;

public class ScheduleOccurrenceExclusion
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public int UserId { get; set; }
    public DateTime OccurrenceAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ScheduleItem Schedule { get; set; } = null!;
    public virtual AppUser User { get; set; } = null!;
}
