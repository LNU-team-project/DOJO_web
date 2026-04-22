using DOJO2.Application.ViewModels;

namespace DOJO2.Application.Interfaces;

public interface ILeaderboardService
{
    Task<LeaderboardViewModel> GetLeaderboardAsync(int limit = 10);
    Task<LeaderboardViewModel> GetLeaderboardBySortAsync(string sortBy = "xp", int limit = 10);
    Task<LeaderboardViewModel> SearchLeaderboardAsync(string searchTerm, int limit = 50);
    Task<LeaderboardViewModel> GetFilteredAndSortedLeaderboardAsync(string searchTerm, string sortBy, int limit = 50);
}