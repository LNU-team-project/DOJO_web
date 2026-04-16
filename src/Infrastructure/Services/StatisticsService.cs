using DOJO2.Infrastructure.Data;
using DOJO2.Infrastructure.Results;
using DOJO2.Application.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace DOJO2.Infrastructure.Services;

public interface IStatisticsService
{
    Task<Result<StatisticsViewModel>> GetTodayStatisticsAsync(int userId, DateTime utcNow);
    Task<Result<DetailedStatisticsViewModel>> GetDetailedStatisticsAsync(int userId, DateTime? startDate = null);
    Task<Result<WeeklyProgressViewModel>> GetWeeklyProgressAsync(int userId, DateTime? dateInWeek = null);
}

public class StatisticsService : IStatisticsService
{
    private readonly IAppDbContext _context;
    private readonly ILogger<StatisticsService> _logger;

    public StatisticsService(IAppDbContext context, ILogger<StatisticsService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<StatisticsViewModel>> GetTodayStatisticsAsync(int userId, DateTime utcNow)
    {
        try
        {
            var dayStartUtc = DateTime.SpecifyKind(utcNow.Date, DateTimeKind.Utc);
            var dayEndUtc = dayStartUtc.AddDays(1);

            var completedTodos = await _context.Tasks
                .Where(t => t.UserId == userId 
                    && t.IsCompleted 
                    && !t.IsPlan 
                    && t.GoalId == null 
                    && t.ParentTaskId == null
                    && t.CompletedAt >= dayStartUtc 
                    && t.CompletedAt < dayEndUtc)
                .CountAsync();

            var completedPlans = await _context.Tasks
                .Where(t => t.UserId == userId 
                    && t.IsCompleted 
                    && t.IsPlan 
                    && t.CompletedAt >= dayStartUtc 
                    && t.CompletedAt < dayEndUtc)
                .CountAsync();

            var pomodoroStats = await _context.Pomodoros
                .Where(p => p.UserId == userId 
                    && p.StartTime >= dayStartUtc 
                    && p.StartTime < dayEndUtc)
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    Sessions = group.Count(),
                    TotalMinutes = group.Sum(p => p.DurationMinutes)
                })
                .FirstOrDefaultAsync();

            var stats = new StatisticsViewModel
            {
                CompletedTodos = completedTodos,
                CompletedPlans = completedPlans,
                CompletedPomodoroSessions = pomodoroStats?.Sessions ?? 0,
                TotalPomodoroMinutes = pomodoroStats?.TotalMinutes ?? 0
            };

            _logger.LogInformation("Статистику за день отримано для користувача {UserId}", userId);
            return Result<StatisticsViewModel>.SuccessResult(stats, "Статистику успішно отримано");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні статистики для користувача {UserId}", userId);
            return Result<StatisticsViewModel>.FailureResult("Помилка при отриманні статистики");
        }
    }

    public async Task<Result<DetailedStatisticsViewModel>> GetDetailedStatisticsAsync(int userId, DateTime? startDate = null)
    {
        try
        {
            var queryStartDate = startDate?.Date ?? DateTime.UtcNow.Date.AddMonths(-1);
            var queryStartDateUtc = DateTime.SpecifyKind(queryStartDate, DateTimeKind.Utc);

            var allTodos = await _context.Tasks
                .Where(t => t.UserId == userId 
                    && !t.IsPlan 
                    && t.GoalId == null 
                    && t.ParentTaskId == null
                    && t.CreatedAt >= queryStartDateUtc)
                .ToListAsync();

            var completedTodos = allTodos.Count(t => t.IsCompleted);
            var totalTodos = allTodos.Count;
            var lastCompletedTodo = allTodos
                .Where(t => t.IsCompleted && t.CompletedAt.HasValue)
                .OrderByDescending(t => t.CompletedAt)
                .FirstOrDefault()
                ?.CompletedAt;

            var allPlans = await _context.Tasks
                .Where(t => t.UserId == userId 
                    && t.IsPlan 
                    && t.CreatedAt >= queryStartDateUtc)
                .ToListAsync();

            var completedPlans = allPlans.Count(t => t.IsCompleted);
            var totalPlans = allPlans.Count;
            var lastCompletedPlan = allPlans
                .Where(t => t.IsCompleted && t.CompletedAt.HasValue)
                .OrderByDescending(t => t.CompletedAt)
                .FirstOrDefault()
                ?.CompletedAt;

            var pomodoroStats = await _context.Pomodoros
                .Where(p => p.UserId == userId && p.StartTime >= queryStartDateUtc)
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    Sessions = group.Count(),
                    TotalMinutes = group.Sum(p => p.DurationMinutes)
                })
                .FirstOrDefaultAsync();

            var stats = new DetailedStatisticsViewModel
            {
                CompletedTodos = completedTodos,
                TotalTodos = totalTodos,
                CompletedPlans = completedPlans,
                TotalPlans = totalPlans,
                CompletedPomodoroSessions = pomodoroStats?.Sessions ?? 0,
                TotalPomodoroMinutes = pomodoroStats?.TotalMinutes ?? 0,
                TotalPomodoroSessions = pomodoroStats?.Sessions ?? 0,
                TodoCompletionRate = totalTodos > 0 ? Math.Round((double)completedTodos / totalTodos * 100, 1) : 0,
                PlanCompletionRate = totalPlans > 0 ? Math.Round((double)completedPlans / totalPlans * 100, 1) : 0,
                LastCompletedTodo = lastCompletedTodo,
                LastCompletedPlan = lastCompletedPlan
            };

            _logger.LogInformation("Детальну статистику отримано для користувача {UserId}", userId);
            return Result<DetailedStatisticsViewModel>.SuccessResult(stats, "Детальну статистику успішно отримано");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні детальної статистики для користувача {UserId}", userId);
            return Result<DetailedStatisticsViewModel>.FailureResult("Помилка при отриманні детальної статистики");
        }
    }

    public async Task<Result<WeeklyProgressViewModel>> GetWeeklyProgressAsync(int userId, DateTime? dateInWeek = null)
    {
        try
        {
            const int DaysInWeek = 7;
            
            // Визначаємо початок тижня (неділя)
            var currentDate = dateInWeek?.Date ?? DateTime.UtcNow.Date;
            var weekStartDate = currentDate.AddDays(-(int)currentDate.DayOfWeek);
            var weekEndDate = weekStartDate.AddDays(DaysInWeek);

            var weekStartDateUtc = DateTime.SpecifyKind(weekStartDate, DateTimeKind.Utc);
            var weekEndDateUtc = DateTime.SpecifyKind(weekEndDate, DateTimeKind.Utc);

            // Отримуємо всі виконання за тиждень
            var todos = await _context.Tasks
                .Where(t => t.UserId == userId 
                    && !t.IsPlan 
                    && t.GoalId == null 
                    && t.ParentTaskId == null
                    && t.IsCompleted
                    && t.CompletedAt.HasValue
                    && t.CompletedAt >= weekStartDateUtc 
                    && t.CompletedAt < weekEndDateUtc)
                .ToListAsync();

            var plans = await _context.Tasks
                .Where(t => t.UserId == userId 
                    && t.IsPlan 
                    && t.IsCompleted
                    && t.CompletedAt.HasValue
                    && t.CompletedAt >= weekStartDateUtc 
                    && t.CompletedAt < weekEndDateUtc)
                .ToListAsync();

            var pomodoros = await _context.Pomodoros
                .Where(p => p.UserId == userId 
                    && p.StartTime >= weekStartDateUtc 
                    && p.StartTime < weekEndDateUtc)
                .ToListAsync();

            // Формуємо денну статистику
            var dailyStatsList = new List<DailyStatisticsViewModel>();
            var dayNames = new[] { "Нд", "Пн", "Вт", "Ср", "Чт", "Пт", "Сб" };

            for (int i = 0; i < DaysInWeek; i++)
            {
                var dayDate = weekStartDate.AddDays(i);
                var dayStartUtc = DateTime.SpecifyKind(dayDate, DateTimeKind.Utc);
                var dayEndUtc = dayStartUtc.AddDays(1);

                var completedTodosForDay = todos.Count(t => t.CompletedAt >= dayStartUtc && t.CompletedAt < dayEndUtc);
                var completedPlansForDay = plans.Count(t => t.CompletedAt >= dayStartUtc && t.CompletedAt < dayEndUtc);
                var pomodorosForDay = pomodoros
                    .Where(p => p.StartTime >= dayStartUtc && p.StartTime < dayEndUtc)
                    .ToList();

                var dailyStat = new DailyStatisticsViewModel
                {
                    Date = dayDate,
                    DayOfWeek = (int)dayDate.DayOfWeek,
                    DayName = dayNames[(int)dayDate.DayOfWeek],
                    CompletedTodos = completedTodosForDay,
                    CompletedPlans = completedPlansForDay,
                    PomodoroSessions = pomodorosForDay.Count,
                    TotalPomodoroMinutes = pomodorosForDay.Sum(p => p.DurationMinutes ?? 0)
                };

                dailyStatsList.Add(dailyStat);
            }

            // Агрегуємо тижневі підсумки
            var totalTodos = todos.Count;
            var totalPlans = plans.Count;
            var totalPomodoros = pomodoros.Count;
            var totalPomodoroMinutes = pomodoros.Sum(p => p.DurationMinutes ?? 0);

            var weeklyStats = new WeeklyProgressViewModel
            {
                WeekStartDate = weekStartDate,
                WeekEndDate = weekEndDate,
                DailyStats = dailyStatsList,
                TotalCompletedTodos = totalTodos,
                TotalCompletedPlans = totalPlans,
                TotalPomodoroSessions = totalPomodoros,
                TotalPomodoroMinutes = totalPomodoroMinutes,
                AverageTodosPerDay = Math.Round((double)totalTodos / DaysInWeek, 1),
                AveragePlansPerDay = Math.Round((double)totalPlans / DaysInWeek, 1),
                AveragePomodoroSessionsPerDay = Math.Round((double)totalPomodoros / DaysInWeek, 1)
            };

            _logger.LogInformation("Тижневу статистику отримано для користувача {UserId}", userId);
            return Result<WeeklyProgressViewModel>.SuccessResult(weeklyStats, "Тижневу статистику успішно отримано");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні тижневої статистики для користувача {UserId}", userId);
            return Result<WeeklyProgressViewModel>.FailureResult("Помилка при отриманні тижневої статистики");
        }
    }
}
