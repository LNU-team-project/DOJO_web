namespace DOJO2.Presentation.ViewModels;


public class TodoCreateViewModel
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short Priority { get; set; } = 2; // 1 = Low, 2 = Medium, 3 = High
    public DateOnly? DueDate { get; set; }
}


public class TodoItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short Priority { get; set; }
    public DateOnly? DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public string PriorityLabel { get; set; } = string.Empty;
}

public class TodoListViewModel
{
    public List<TodoItemViewModel> IncompleteTodos { get; set; } = new();
    public List<TodoItemViewModel> CompletedTodos { get; set; } = new();
}

