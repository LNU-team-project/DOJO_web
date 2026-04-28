using DOJO2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DOJO2.Infrastructure.Data;

public interface IAppDbContext
{
    DbSet<TaskItem> Tasks { get; }
    DbSet<ScheduleItem> Schedules { get; }
    DbSet<ScheduleOccurrenceExclusion> ScheduleExclusions { get; }
    DbSet<Attachment> Attachments { get; }
    DbSet<Friend> Friends { get; }
    DbSet<Pomodoro> Pomodoros { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
