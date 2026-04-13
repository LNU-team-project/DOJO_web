namespace DOJO2.Presentation.ViewModels;

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

