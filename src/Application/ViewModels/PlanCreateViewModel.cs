namespace DOJO2.Application.ViewModels;

public class PlanCreateViewModel
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public short Priority { get; set; } = 2;
}

public class PlanItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public short Priority { get; set; }
    public bool IsCompleted { get; set; }
    public string PriorityLabel { get; set; } = string.Empty;
}

public class PlanListViewModel
{
    public List<PlanItemViewModel> IncompletePlans { get; set; } = new();
    public List<PlanItemViewModel> CompletedPlans { get; set; } = new();
}

public class PlanAttachmentItemViewModel
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
