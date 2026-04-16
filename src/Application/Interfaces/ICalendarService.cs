using DOJO2.Infrastructure.Results;

namespace DOJO2.Application.Interfaces;

public interface ICalendarService
{
    Task<Result<List<string>>> GetMarkedDatesAsync(int userId, DateOnly from, DateOnly to);
}
