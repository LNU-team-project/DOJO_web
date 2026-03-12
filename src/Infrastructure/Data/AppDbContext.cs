using DOJO2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DOJO2.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Pomodoro> Pomodoros => Set<Pomodoro>();
    public DbSet<Attachment> Attachments => Set<Attachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── User ──────────────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            e.Property(u => u.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            e.Property(u => u.UserName).HasColumnName("user_name").HasMaxLength(100).IsRequired();
            e.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired();
            e.Property(u => u.ExpPoints).HasColumnName("exp_points").HasDefaultValue(0);
            e.Property(u => u.Level).HasColumnName("level").HasDefaultValue(1);
            e.Property(u => u.CurrentStreak).HasColumnName("current_streak").HasDefaultValue(0);
            e.Property(u => u.LastCompletionDate).HasColumnName("last_completion_date");
            e.Property(u => u.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

            e.HasIndex(u => u.Email).IsUnique();
            e.HasIndex(u => u.UserName).IsUnique();
        });

        // ── Goal ──────────────────────────────────────────────
        modelBuilder.Entity<Goal>(e =>
        {
            e.ToTable("goals", t =>
            {
                t.HasCheckConstraint("chk_goal_priority", "priority BETWEEN 1 AND 3");
                t.HasCheckConstraint("chk_goal_progress", "progress BETWEEN 0 AND 100");
            });
            e.HasKey(g => g.Id);
            e.Property(g => g.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            e.Property(g => g.UserId).HasColumnName("user_id");
            e.Property(g => g.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
            e.Property(g => g.Description).HasColumnName("description");
            e.Property(g => g.Deadline).HasColumnName("deadline");
            e.Property(g => g.Priority).HasColumnName("priority").HasDefaultValue((short)2);
            e.Property(g => g.Progress).HasColumnName("progress").HasDefaultValue((short)0);
            e.Property(g => g.IsCompleted).HasColumnName("is_completed").HasDefaultValue(false);
            e.Property(g => g.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            e.Property(g => g.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

            e.HasOne(g => g.User)
                .WithMany(u => u.Goals)
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(g => g.UserId);
            e.HasIndex(g => g.IsCompleted);
        });

        // ── TaskItem ──────────────────────────────────────────
        modelBuilder.Entity<TaskItem>(e =>
        {
            e.ToTable("tasks", tb =>
            {
                tb.HasCheckConstraint("chk_task_priority", "priority BETWEEN 1 AND 3");
                tb.HasCheckConstraint("chk_no_self_parent", "id <> parent_task_id");
            });
            e.HasKey(task => task.Id);
            e.Property(task => task.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            e.Property(task => task.UserId).HasColumnName("user_id");
            e.Property(task => task.GoalId).HasColumnName("goal_id");
            e.Property(task => task.ParentTaskId).HasColumnName("parent_task_id");
            e.Property(task => task.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
            e.Property(task => task.Description).HasColumnName("description");
            e.Property(task => task.IsCompleted).HasColumnName("is_completed").HasDefaultValue(false);
            e.Property(task => task.DueDate).HasColumnName("due_date");
            e.Property(task => task.Priority).HasColumnName("priority").HasDefaultValue((short)2);
            e.Property(task => task.CompletedAt).HasColumnName("completed_at");
            e.Property(task => task.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

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
        });

        // ── Pomodoro ──────────────────────────────────────────
        modelBuilder.Entity<Pomodoro>(e =>
        {
            e.ToTable("pomodoros", t =>
            {
                t.HasCheckConstraint("chk_pomodoro_duration", "duration_minutes > 0");
                t.HasCheckConstraint("chk_pomodoro_work_cycles", "work_cycles > 0");
                t.HasCheckConstraint("chk_pomodoro_end_after_start", "end_time IS NULL OR end_time > start_time");
            });
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            e.Property(p => p.UserId).HasColumnName("user_id");
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
        modelBuilder.Entity<Attachment>(e =>
        {
            e.ToTable("attachments");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).HasColumnName("id").UseIdentityAlwaysColumn();
            e.Property(a => a.TaskId).HasColumnName("task_id");
            e.Property(a => a.FileName).HasColumnName("file_name").HasMaxLength(255).IsRequired();
            e.Property(a => a.FilePath).HasColumnName("file_path").IsRequired();
            e.Property(a => a.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

            e.HasOne(a => a.Task)
                .WithMany(t => t.Attachments)
                .HasForeignKey(a => a.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(a => a.TaskId);
        });
    }
}

