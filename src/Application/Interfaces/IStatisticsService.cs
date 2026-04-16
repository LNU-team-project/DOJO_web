using DOJO2.Application.ViewModels;
using DOJO2.Infrastructure.Results;

namespace DOJO2.Application.Interfaces;

public interface IStatisticsService
{
    Task<Result<StatisticsViewModel>> GetTodayStatisticsAsync(int userId, DateTime utcNow);
    Task<Result<DetailedStatisticsViewModel>> GetDetailedStatisticsAsync(int userId, DateTime? startDate = null);
    Task<Result<WeeklyProgressViewModel>> GetWeeklyProgressAsync(int userId, DateTime? dateInWeek = null);
}
