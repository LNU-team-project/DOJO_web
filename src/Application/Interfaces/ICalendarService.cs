using DOJO2.Application.Common;

namespace DOJO2.Application.Interfaces;

public interface ICalendarService
{
    Task<Result<List<string>>> GetMarkedDatesAsync(int userId, DateOnly from, DateOnly to);
}
