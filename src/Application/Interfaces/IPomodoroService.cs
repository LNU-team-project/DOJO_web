using DOJO2.Application.ViewModels;
using DOJO2.Infrastructure.Results;

namespace DOJO2.Application.Interfaces;

public interface IPomodoroService
{
    Task<Result<PomodoroTodayStatsViewModel>> GetTodayStatsAsync(int userId, DateTime utcNow);
    Task<Result<PomodoroTodayStatsViewModel>> CreateSessionAsync(int userId, PomodoroSessionCreateViewModel? model);
}
