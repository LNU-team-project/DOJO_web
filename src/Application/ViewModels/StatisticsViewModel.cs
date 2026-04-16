namespace DOJO2.Application.ViewModels;

public class StatisticsViewModel
{
    public int CompletedTodos { get; set; }
    public int CompletedPlans { get; set; }
    public int CompletedPomodoroSessions { get; set; }
    public int TotalPomodoroMinutes { get; set; }
}

public class DetailedStatisticsViewModel
{
    public int CompletedTodos { get; set; }
    public int TotalTodos { get; set; }
    public int CompletedPlans { get; set; }
    public int TotalPlans { get; set; }
    public int CompletedPomodoroSessions { get; set; }
    public int TotalPomodoroMinutes { get; set; }
    public int TotalPomodoroSessions { get; set; }
    public double TodoCompletionRate { get; set; }
    public double PlanCompletionRate { get; set; }
    public DateTime? LastCompletedTodo { get; set; }
    public DateTime? LastCompletedPlan { get; set; }
}

public class DailyStatisticsViewModel
{
    public DateTime Date { get; set; }
    public int DayOfWeek { get; set; } // 0 = Sunday, 1 = Monday, etc.
    public string? DayName { get; set; } // "Пн", "Вт"
    public int CompletedTodos { get; set; }
    public int CompletedPlans { get; set; }
    public int PomodoroSessions { get; set; }
    public int TotalPomodoroMinutes { get; set; }
}

public class WeeklyProgressViewModel
{
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }
    public List<DailyStatisticsViewModel> DailyStats { get; set; } = new();
    
    // Загальні показники за тиждень
    public int TotalCompletedTodos { get; set; }
    public int TotalCompletedPlans { get; set; }
    public int TotalPomodoroSessions { get; set; }
    public int TotalPomodoroMinutes { get; set; }
    
    // Середні показники
    public double AverageTodosPerDay { get; set; }
    public double AveragePlansPerDay { get; set; }
    public double AveragePomodoroSessionsPerDay { get; set; }
}
