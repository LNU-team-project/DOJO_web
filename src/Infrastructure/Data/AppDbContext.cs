using DOJO2.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DOJO2.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<int>, int>, IAppDbContext
{
    private const string CreatedAtColumnName = "created_at";
    private const string NowSqlExpression = "NOW()";
    private const string UserIdColumnName = "user_id";

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<ScheduleItem> Schedules => Set<ScheduleItem>();
    public DbSet<ScheduleOccurrenceExclusion> ScheduleExclusions => Set<ScheduleOccurrenceExclusion>();
    public DbSet<Pomodoro> Pomodoros => Set<Pomodoro>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Admin> Admins => Set<Admin>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ── AppUser (Identity) ────────────────────────────────
        builder.Entity<AppUser>(e =>
        {
            e.ToTable("users");
            e.Property(u => u.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            e.Property(u => u.UserName).HasColumnName("user_name").HasMaxLength(100).IsRequired();
            e.Property(u => u.NormalizedUserName).HasColumnName("normalized_user_name").HasMaxLength(100);
            e.Property(u => u.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            e.Property(u => u.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(255);
            e.Property(u => u.EmailConfirmed).HasColumnName("email_confirmed");
            e.Property(u => u.PasswordHash).HasColumnName("password_hash");
            e.Property(u => u.SecurityStamp).HasColumnName("security_stamp");
            e.Property(u => u.ConcurrencyStamp).HasColumnName("concurrency_stamp");
            e.Property(u => u.PhoneNumber).HasColumnName("phone_number");
            e.Property(u => u.PhoneNumberConfirmed).HasColumnName("phone_number_confirmed");
            e.Property(u => u.TwoFactorEnabled).HasColumnName("two_factor_enabled");
            e.Property(u => u.LockoutEnd).HasColumnName("lockout_end");
            e.Property(u => u.LockoutEnabled).HasColumnName("lockout_enabled");
            e.Property(u => u.AccessFailedCount).HasColumnName("access_failed_count");
            e.Property(u => u.ExpPoints).HasColumnName("exp_points").HasDefaultValue(0);
            e.Property(u => u.Level).HasColumnName("level").HasDefaultValue(1);
            e.Property(u => u.CurrentStreak).HasColumnName("current_streak").HasDefaultValue(0);
            e.Property(u => u.LastCompletionDate).HasColumnName("last_completion_date");
            e.Property(u => u.CreatedAt).HasColumnName(CreatedAtColumnName).HasDefaultValueSql(NowSqlExpression);

            e.HasIndex(u => u.NormalizedEmail).HasDatabaseName("EmailIndex");
            e.HasIndex(u => u.NormalizedUserName).IsUnique().HasDatabaseName("UserNameIndex");
        });

        // ── Goal ──────────────────────────────────────────────
        builder.Entity<Goal>(e =>
        {
            e.ToTable("goals", t =>
            {
                t.HasCheckConstraint("chk_goal_priority", "priority BETWEEN 1 AND 3");
                t.HasCheckConstraint("chk_goal_progress", "progress BETWEEN 0 AND 100");
            });
            e.HasKey(g => g.Id);
            e.Property(g => g.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            e.Property(g => g.UserId).HasColumnName(UserIdColumnName);
            e.Property(g => g.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
            e.Property(g => g.Description).HasColumnName("description");
            e.Property(g => g.Deadline).HasColumnName("deadline");
            e.Property(g => g.Priority).HasColumnName("priority").HasDefaultValue((short)2);
            e.Property(g => g.Progress).HasColumnName("progress").HasDefaultValue((short)0);
            e.Property(g => g.IsCompleted).HasColumnName("is_completed").HasDefaultValue(false);
            e.Property(g => g.CreatedAt).HasColumnName(CreatedAtColumnName).HasDefaultValueSql(NowSqlExpression);
            e.Property(g => g.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql(NowSqlExpression);

            e.HasOne(g => g.User)
                .WithMany(u => u.Goals)
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(g => g.UserId);
            e.HasIndex(g => g.IsCompleted);
        });

        // ── Admin ──────────────────────────────────────────────
        builder.Entity<Admin>(e =>
        {
            e.ToTable("admins");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            e.Property(a => a.Login).HasColumnName("login").HasMaxLength(100).IsRequired();
            e.Property(a => a.Password).HasColumnName("password").IsRequired();
            e.HasIndex(a => a.Login).IsUnique();
        });

        // ── TaskItem ──────────────────────────────────────────
        builder.Entity<TaskItem>(e =>
        {
            e.ToTable("tasks", tb =>
            {
                tb.HasCheckConstraint("chk_task_priority", "priority BETWEEN 1 AND 3");
                tb.HasCheckConstraint("chk_no_self_parent", "id <> parent_task_id");
            });
            e.HasKey(task => task.Id);
            e.Property(task => task.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            e.Property(task => task.UserId).HasColumnName(UserIdColumnName);
            e.Property(task => task.GoalId).HasColumnName("goal_id");
            e.Property(task => task.ParentTaskId).HasColumnName("parent_task_id");
            e.Property(task => task.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
            e.Property(task => task.Description).HasColumnName("description");
            e.Property(task => task.IsCompleted).HasColumnName("is_completed").HasDefaultValue(false);
            e.Property(task => task.DueDate).HasColumnName("due_date");
            e.Property(task => task.XpAwarded).HasColumnName("xp_awarded").HasDefaultValue(false);
            e.Property(task => task.Priority).HasColumnName("priority").HasDefaultValue((short)2);
            e.Property(task => task.CompletedAt).HasColumnName("completed_at");
            e.Property(task => task.CreatedAt).HasColumnName(CreatedAtColumnName).HasDefaultValueSql(NowSqlExpression);
            e.Property(task => task.IsPlan).HasColumnName("is_plan").HasDefaultValue(false);
            e.Property(task => task.ScheduledAt).HasColumnName("scheduled_at");

            e.HasOne(task => task.User)
                .WithMany(u => u.Tasks)
                .HasForeignKey(task => task.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(task => task.Goal)
                .WithMany(g => g.Tasks)
                .HasForeignKey(task => task.GoalId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(task => task.ParentTask)
                .WithMany(task => task.SubTasks)
                .HasForeignKey(task => task.ParentTaskId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(task => task.UserId);
            e.HasIndex(task => task.GoalId);
            e.HasIndex(task => task.ParentTaskId);
            e.HasIndex(task => task.IsCompleted);
            e.HasIndex(task => task.IsPlan);
        });

        // ── ScheduleItem ─────────────────────────────────────
        builder.Entity<ScheduleItem>(e =>
        {
            e.ToTable("schedules", tb =>
            {
                tb.HasCheckConstraint("chk_schedule_priority", "priority BETWEEN 1 AND 3");
                tb.HasCheckConstraint("chk_schedule_interval", "recurrence_interval > 0");
                tb.HasCheckConstraint("chk_schedule_week_mask", "weekly_days_mask BETWEEN 0 AND 127");
            });

            e.HasKey(schedule => schedule.Id);
            e.Property(schedule => schedule.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            e.Property(schedule => schedule.UserId).HasColumnName(UserIdColumnName);
            e.Property(schedule => schedule.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
            e.Property(schedule => schedule.Description).HasColumnName("description");
            e.Property(schedule => schedule.StartAt).HasColumnName("start_at");
            e.Property(schedule => schedule.DurationMinutes).HasColumnName("duration_minutes").HasDefaultValue((short)60);
            e.Property(schedule => schedule.Priority).HasColumnName("priority").HasDefaultValue((short)2);
            e.Property(schedule => schedule.RecurrenceType).HasColumnName("recurrence_type").HasMaxLength(16).HasDefaultValue("none");
            e.Property(schedule => schedule.RecurrenceInterval).HasColumnName("recurrence_interval").HasDefaultValue((short)1);
            e.Property(schedule => schedule.WeeklyDaysMask).HasColumnName("weekly_days_mask").HasDefaultValue((short)0);
            e.Property(schedule => schedule.RecurrenceEndDate).HasColumnName("recurrence_end_date");
            e.Property(schedule => schedule.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            e.Property(schedule => schedule.CreatedAt).HasColumnName(CreatedAtColumnName).HasDefaultValueSql(NowSqlExpression);

            e.HasOne(schedule => schedule.User)
                .WithMany()
                .HasForeignKey(schedule => schedule.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(schedule => schedule.UserId);
            e.HasIndex(schedule => schedule.StartAt);
            e.HasIndex(schedule => schedule.IsActive);
        });

        // ── ScheduleOccurrenceExclusion ─────────────────────
        builder.Entity<ScheduleOccurrenceExclusion>(e =>
        {
            e.ToTable("schedule_occurrence_exceptions");

            e.HasKey(exception => exception.Id);
            e.Property(exception => exception.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            e.Property(exception => exception.ScheduleId).HasColumnName("schedule_id");
            e.Property(exception => exception.UserId).HasColumnName(UserIdColumnName);
            e.Property(exception => exception.OccurrenceAt).HasColumnName("occurrence_at");
            e.Property(exception => exception.CreatedAt).HasColumnName(CreatedAtColumnName).HasDefaultValueSql(NowSqlExpression);

            e.HasOne(exception => exception.Schedule)
                .WithMany()
                .HasForeignKey(exception => exception.ScheduleId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(exception => exception.User)
                .WithMany()
                .HasForeignKey(exception => exception.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(exception => exception.ScheduleId);
            e.HasIndex(exception => exception.UserId);
            e.HasIndex(exception => exception.OccurrenceAt);
            e.HasIndex(exception => new { exception.ScheduleId, exception.OccurrenceAt }).IsUnique();
        });

        // ── Pomodoro ──────────────────────────────────────────
        builder.Entity<Pomodoro>(e =>
        {
            e.ToTable("pomodoros", t =>
            {
                t.HasCheckConstraint("chk_pomodoro_duration", "duration_minutes > 0");
                t.HasCheckConstraint("chk_pomodoro_work_cycles", "work_cycles > 0");
                t.HasCheckConstraint("chk_pomodoro_end_after_start", "end_time IS NULL OR end_time > start_time");
            });
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            e.Property(p => p.UserId).HasColumnName(UserIdColumnName);
            e.Property(p => p.TaskId).HasColumnName("task_id");
            e.Property(p => p.StartTime).HasColumnName("start_time");
            e.Property(p => p.EndTime).HasColumnName("end_time");
            e.Property(p => p.DurationMinutes).HasColumnName("duration_minutes");
            e.Property(p => p.WorkCycles).HasColumnName("work_cycles").HasDefaultValue((short)1);

            e.HasOne(p => p.User)
                .WithMany(u => u.Pomodoros)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.Task)
                .WithMany(t => t.Pomodoros)
                .HasForeignKey(p => p.TaskId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(p => p.UserId);
            e.HasIndex(p => p.TaskId);
        });

        // ── Attachment ────────────────────────────────────────
        builder.Entity<Attachment>(e =>
        {
            e.ToTable("attachments");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            e.Property(a => a.TaskId).HasColumnName("task_id");
            e.Property(a => a.FileName).HasColumnName("file_name").HasMaxLength(255).IsRequired();
            e.Property(a => a.FilePath).HasColumnName("file_path").IsRequired();
            e.Property(a => a.CreatedAt).HasColumnName(CreatedAtColumnName).HasDefaultValueSql(NowSqlExpression);

            e.HasOne(a => a.Task)
                .WithMany(t => t.Attachments)
                .HasForeignKey(a => a.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(a => a.TaskId);
        });
    }
}
