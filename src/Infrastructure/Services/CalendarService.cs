using DOJO2.Infrastructure.Data;
using DOJO2.Infrastructure.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace DOJO2.Infrastructure.Services;

public interface ICalendarService
{
    Task<Result<List<string>>> GetMarkedDatesAsync(int userId, DateOnly from, DateOnly to);
}

public class CalendarService : ICalendarService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CalendarService> _logger;

    public CalendarService(AppDbContext context, ILogger<CalendarService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<List<string>>> GetMarkedDatesAsync(int userId, DateOnly from, DateOnly to)
    {
        if (userId <= 0)
        {
            return Result<List<string>>.FailureResult("Користувача не знайдено");
        }

        if (from > to)
        {
            return Result<List<string>>.FailureResult("Дата 'from' не може бути пізніше за 'to'");
        }

        var fromDate = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDate = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var marks = await _context.Tasks
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.IsPlan && t.ScheduledAt.HasValue)
            .Where(t => t.ScheduledAt!.Value >= fromDate && t.ScheduledAt!.Value <= toDate)
            .Select(t => t.ScheduledAt!.Value.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();

        var markStrings = marks
            .Select(d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            .ToList();

        return Result<List<string>>.SuccessResult(markStrings, "Позначки календаря отримано");
    }
}
