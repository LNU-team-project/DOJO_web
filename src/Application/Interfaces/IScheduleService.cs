using DOJO2.Application.Common;
using DOJO2.Application.ViewModels;

namespace DOJO2.Application.Interfaces;

public interface IScheduleService
{
    Task<Result<ScheduleItemViewModel>> CreateScheduleAsync(int userId, ScheduleCreateViewModel? model);
    Task<Result<List<ScheduleOccurrenceViewModel>>> GetSchedulesForRangeAsync(int userId, DateTime? weekStart, DateTime? weekEnd);
}
