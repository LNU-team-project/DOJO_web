namespace DOJO2.Domain.Entities;

public class Attachment
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public string FileName { get; set; } = null!;
    public string FilePath { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public TaskItem Task { get; set; } = null!;
}

