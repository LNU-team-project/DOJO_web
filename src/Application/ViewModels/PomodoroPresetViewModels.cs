using System.ComponentModel.DataAnnotations;

namespace DOJO2.Application.ViewModels;

public class PomodoroPresetCreateViewModel
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 180)]
    public short FocusMinutes { get; set; }

    [Range(1, 60)]
    public short ShortBreakMinutes { get; set; }

    [Range(1, 120)]
    public short LongBreakMinutes { get; set; }
}

public class PomodoroPresetViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public short FocusMinutes { get; set; }
    public short ShortBreakMinutes { get; set; }
    public short LongBreakMinutes { get; set; }
    public DateTime CreatedAt { get; set; }
}