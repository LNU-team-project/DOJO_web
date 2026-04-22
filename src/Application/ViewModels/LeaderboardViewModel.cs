using System.Collections.Generic;

namespace DOJO2.Application.ViewModels
{
    public class LeaderboardEntry
    {
        public int Rank { get; set; }
        public string? Username { get; set; }
        public int Score { get; set; }
        public string? AvatarUrl { get; set; }
        public int Level { get; set; }
        public int PomodoroSessions { get; set; }
    }

    public class LeaderboardViewModel
    {
        public List<LeaderboardEntry> Entries { get; set; } = new List<LeaderboardEntry>();
    }
}
