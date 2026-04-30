using DOJO2.Application.ViewModels;
using DOJO2.Application.Common;

namespace DOJO2.Application.Interfaces;

public interface IPomodoroService
{
    Task<Result<PomodoroTodayStatsViewModel>> GetTodayStatsAsync(int userId, DateTime utcNow);
    Task<Result<PomodoroTodayStatsViewModel>> CreateSessionAsync(int userId, PomodoroSessionCreateViewModel? model);
    Task<Result<IReadOnlyList<PomodoroPresetViewModel>>> GetPresetsAsync(int userId);
    Task<Result<PomodoroPresetViewModel>> CreatePresetAsync(int userId, PomodoroPresetCreateViewModel? model);
    Task<Result<PomodoroPresetViewModel>> UpdatePresetAsync(int userId, int presetId, PomodoroPresetCreateViewModel? model);
    Task<Result<bool>> DeletePresetAsync(int userId, int presetId);
}
