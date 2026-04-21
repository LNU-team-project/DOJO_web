namespace DOJO2.Application.ViewModels;

public class HeroStatusViewModel
{
    public int Level { get; set; }
    public int ExpPoints { get; set; }
    public int ExpToNextLevel { get; set; }
    public int ProgressPercent { get; set; }
    public int CurrentStreak { get; set; }
    public string StreakText { get; set; } = string.Empty;
    // Чи був перехід на новий рівень під час останньої операції
    public bool HasLeveledUp { get; set; }
    // Скільки рівнів піднято (може бути >1 якщо нарахували багато XP одночасно)
    public int LevelsGained { get; set; }
    // Скільки XP залишилось до наступного рівня (поточний рівень)
    public int ExpToLevelRemaining { get; set; }
}
