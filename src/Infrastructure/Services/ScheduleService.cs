using DOJO2.Application.Common;
using DOJO2.Application.Interfaces;
using DOJO2.Application.ViewModels;
using DOJO2.Domain.Entities;
using DOJO2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DOJO2.Infrastructure.Services;

public class ScheduleService : IScheduleService
{
    private const int WeeklySundayMask = 1;
    private const string RecurrenceNone = "none";
    private const string RecurrenceDaily = "daily";
    private const string RecurrenceWeekly = "weekly";
    private const string RecurrenceMonthly = "monthly";

    public ScheduleService(IAppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    private readonly IAppDbContext _context;

    public async Task<Result<ScheduleItemViewModel>> CreateScheduleAsync(int userId, ScheduleCreateViewModel? model)
    {
        if (model == null)
        {
            return Result<ScheduleItemViewModel>.FailureResult("Модель розкладу не може бути порожньою");
        }

        if (string.IsNullOrWhiteSpace(model.Title))
        {
            return Result<ScheduleItemViewModel>.FailureResult("Назва розкладу не може бути порожньою");
        }

        if (model.Title.Length > 255)
        {
            return Result<ScheduleItemViewModel>.FailureResult("Назва розкладу не може перевищувати 255 символів");
        }

        if (model.StartAt == null)
        {
            return Result<ScheduleItemViewModel>.FailureResult("Оберіть дату та час розкладу");
        }

        if (model.DurationMinutes < 5 || model.DurationMinutes > 720)
        {
            return Result<ScheduleItemViewModel>.FailureResult("Тривалість має бути від 5 до 720 хвилин");
        }

        var recurrenceType = NormalizeRecurrenceType(model.RecurrenceType);
        if (recurrenceType == null)
        {
            return Result<ScheduleItemViewModel>.FailureResult("Недопустимий тип повторення");
        }

        if (model.RecurrenceInterval < 1 || model.RecurrenceInterval > 30)
        {
            return Result<ScheduleItemViewModel>.FailureResult("Інтервал повторення має бути від 1 до 30");
        }

        var weeklyMask = BuildWeeklyMask(model.WeeklyDays);
        if (recurrenceType == RecurrenceWeekly && weeklyMask == 0)
        {
            weeklyMask = DayToMask(model.StartAt.Value.DayOfWeek);
        }

        var entity = new ScheduleItem
        {
            UserId = userId,
            Title = model.Title.Trim(),
            Description = model.Description?.Trim(),
            StartAt = DateTime.SpecifyKind(model.StartAt.Value, DateTimeKind.Utc),
            DurationMinutes = model.DurationMinutes,
            Priority = model.Priority,
            RecurrenceType = recurrenceType,
            RecurrenceInterval = model.RecurrenceInterval,
            WeeklyDaysMask = weeklyMask,
            RecurrenceEndDate = model.RecurrenceEndDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Schedules.Add(entity);
        await _context.SaveChangesAsync();

        return Result<ScheduleItemViewModel>.SuccessResult(MapToItemViewModel(entity), "Розклад створено");
    }

    public async Task<Result<List<ScheduleOccurrenceViewModel>>> GetSchedulesForRangeAsync(int userId, DateTime? weekStart, DateTime? weekEnd)
    {
        if (weekStart == null || weekEnd == null)
        {
            return Result<List<ScheduleOccurrenceViewModel>>.FailureResult("Діапазон дат не задано");
        }

        var start = DateTime.SpecifyKind(weekStart.Value, DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(weekEnd.Value, DateTimeKind.Utc);
        if (end < start)
        {
            return Result<List<ScheduleOccurrenceViewModel>>.FailureResult("Кінець діапазону не може бути раніше початку");
        }

        var schedules = await _context.Schedules
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.IsActive)
            .ToListAsync();

        var occurrences = new List<ScheduleOccurrenceViewModel>();

        foreach (var schedule in schedules)
        {
            occurrences.AddRange(BuildOccurrencesForRange(schedule, start, end));
        }

        var ordered = occurrences
            .OrderBy(x => x.OccurrenceAt)
            .ThenBy(x => x.Title)
            .ToList();

        return Result<List<ScheduleOccurrenceViewModel>>.SuccessResult(ordered, "Розклад завантажено");
    }

    private static List<ScheduleOccurrenceViewModel> BuildOccurrencesForRange(ScheduleItem schedule, DateTime rangeStart, DateTime rangeEnd)
    {
        var results = new List<ScheduleOccurrenceViewModel>();
        var recurrenceType = NormalizeRecurrenceType(schedule.RecurrenceType) ?? RecurrenceNone;

        if (recurrenceType == RecurrenceNone)
        {
            if (schedule.StartAt >= rangeStart && schedule.StartAt <= rangeEnd && IsBeforeEndDate(schedule, schedule.StartAt))
            {
                results.Add(MapToOccurrenceViewModel(schedule, schedule.StartAt));
            }

            return results;
        }

        if (recurrenceType == RecurrenceDaily)
        {
            AddDailyOccurrences(schedule, rangeStart, rangeEnd, results);
            return results;
        }

        if (recurrenceType == RecurrenceWeekly)
        {
            AddWeeklyOccurrences(schedule, rangeStart, rangeEnd, results);
            return results;
        }

        if (recurrenceType == RecurrenceMonthly)
        {
            AddMonthlyOccurrences(schedule, rangeStart, rangeEnd, results);
            return results;
        }

        return results;
    }

    private static void AddDailyOccurrences(ScheduleItem schedule, DateTime rangeStart, DateTime rangeEnd, List<ScheduleOccurrenceViewModel> target)
    {
        var current = schedule.StartAt;
        var step = Math.Max(schedule.RecurrenceInterval, (short)1);

        if (current < rangeStart)
        {
            var diffDays = (int)Math.Floor((rangeStart - current).TotalDays);
            var jumps = diffDays / step;
            current = current.AddDays(jumps * step);
            while (current < rangeStart)
            {
                current = current.AddDays(step);
            }
        }

        while (current <= rangeEnd)
        {
            if (!IsBeforeEndDate(schedule, current))
            {
                break;
            }

            target.Add(MapToOccurrenceViewModel(schedule, current));
            current = current.AddDays(step);
        }
    }

    private static void AddWeeklyOccurrences(ScheduleItem schedule, DateTime rangeStart, DateTime rangeEnd, List<ScheduleOccurrenceViewModel> target)
    {
        var mask = schedule.WeeklyDaysMask;
        if (mask == 0)
        {
            mask = DayToMask(schedule.StartAt.DayOfWeek);
        }

        var weekStart = rangeStart.Date;
        while (weekStart.DayOfWeek != DayOfWeek.Monday)
        {
            weekStart = weekStart.AddDays(-1);
        }

        var finalDate = rangeEnd.Date;
        var interval = Math.Max(schedule.RecurrenceInterval, (short)1);
        var anchorWeekStart = schedule.StartAt.Date;
        while (anchorWeekStart.DayOfWeek != DayOfWeek.Monday)
        {
            anchorWeekStart = anchorWeekStart.AddDays(-1);
        }

        for (var day = weekStart; day <= finalDate; day = day.AddDays(1))
        {
            if (!ShouldIncludeWeeklyDay(schedule, day, mask, anchorWeekStart, interval))
            {
                continue;
            }

            var occurrence = day
                .AddHours(schedule.StartAt.Hour)
                .AddMinutes(schedule.StartAt.Minute);

            if (occurrence < rangeStart || occurrence > rangeEnd || !IsBeforeEndDate(schedule, occurrence))
            {
                continue;
            }

            target.Add(MapToOccurrenceViewModel(schedule, occurrence));
        }
    }

    private static bool ShouldIncludeWeeklyDay(
        ScheduleItem schedule,
        DateTime day,
        short mask,
        DateTime anchorWeekStart,
        int interval)
    {
        if (day < schedule.StartAt.Date)
        {
            return false;
        }

        var dayMask = DayToMask(day.DayOfWeek);
        if ((mask & dayMask) == 0)
        {
            return false;
        }

        var weeksFromAnchor = (int)((day - anchorWeekStart).TotalDays / 7);
        return weeksFromAnchor >= 0 && weeksFromAnchor % interval == 0;
    }

    private static void AddMonthlyOccurrences(ScheduleItem schedule, DateTime rangeStart, DateTime rangeEnd, List<ScheduleOccurrenceViewModel> target)
    {
        var interval = Math.Max(schedule.RecurrenceInterval, (short)1);
        var anchor = new DateTime(schedule.StartAt.Year, schedule.StartAt.Month, 1, schedule.StartAt.Hour, schedule.StartAt.Minute, 0, DateTimeKind.Utc);
        var targetMonth = new DateTime(rangeStart.Year, rangeStart.Month, 1, schedule.StartAt.Hour, schedule.StartAt.Minute, 0, DateTimeKind.Utc);

        if (targetMonth < anchor)
        {
            targetMonth = anchor;
        }

        var monthsDiff = (targetMonth.Year - anchor.Year) * 12 + targetMonth.Month - anchor.Month;
        var jumps = monthsDiff / interval;
        var currentMonth = anchor.AddMonths(jumps * interval);

        while (currentMonth <= rangeEnd)
        {
            var occurrenceDay = schedule.StartAt.Day;
            var daysInMonth = DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month);
            if (occurrenceDay <= daysInMonth)
            {
                var occurrence = new DateTime(
                    currentMonth.Year,
                    currentMonth.Month,
                    occurrenceDay,
                    schedule.StartAt.Hour,
                    schedule.StartAt.Minute,
                    0,
                    DateTimeKind.Utc);

                if (occurrence >= schedule.StartAt && occurrence >= rangeStart && occurrence <= rangeEnd && IsBeforeEndDate(schedule, occurrence))
                {
                    target.Add(MapToOccurrenceViewModel(schedule, occurrence));
                }
            }

            currentMonth = currentMonth.AddMonths(interval);
        }
    }

    private static bool IsBeforeEndDate(ScheduleItem schedule, DateTime occurrence)
    {
        if (schedule.RecurrenceEndDate == null)
        {
            return true;
        }

        return occurrence.Date <= schedule.RecurrenceEndDate.Value.ToDateTime(TimeOnly.MaxValue).Date;
    }

    private static string? NormalizeRecurrenceType(string? recurrenceType)
    {
        var value = recurrenceType?.Trim().ToLowerInvariant();
        return value switch
        {
            RecurrenceNone => RecurrenceNone,
            RecurrenceDaily => RecurrenceDaily,
            RecurrenceWeekly => RecurrenceWeekly,
            RecurrenceMonthly => RecurrenceMonthly,
            _ => null
        };
    }

    private static short BuildWeeklyMask(IEnumerable<int>? weeklyDays)
    {
        if (weeklyDays == null)
        {
            return 0;
        }

        short mask = 0;
        foreach (var day in weeklyDays)
        {
            if (day < 0 || day > 6)
            {
                continue;
            }

            mask = (short)(mask | DayToMask((DayOfWeek)day));
        }

        return mask;
    }

    private static List<int> DecodeWeeklyDays(short mask)
    {
        var days = new List<int>();
        for (var day = 0; day <= 6; day++)
        {
            var dayMask = DayToMask((DayOfWeek)day);
            if ((mask & dayMask) != 0)
            {
                days.Add(day);
            }
        }

        return days;
    }

    private static short DayToMask(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Sunday => WeeklySundayMask,
            DayOfWeek.Monday => 2,
            DayOfWeek.Tuesday => 4,
            DayOfWeek.Wednesday => 8,
            DayOfWeek.Thursday => 16,
            DayOfWeek.Friday => 32,
            DayOfWeek.Saturday => 64,
            _ => 0
        };
    }

    private static string GetPriorityLabel(short priority)
    {
        return priority switch
        {
            1 => "Низька",
            2 => "Середня",
            3 => "Висока",
            _ => "Невідома"
        };
    }

    private static ScheduleItemViewModel MapToItemViewModel(ScheduleItem schedule)
    {
        return new ScheduleItemViewModel
        {
            Id = schedule.Id,
            Title = schedule.Title,
            Description = schedule.Description,
            StartAt = schedule.StartAt,
            DurationMinutes = schedule.DurationMinutes,
            Priority = schedule.Priority,
            PriorityLabel = GetPriorityLabel(schedule.Priority),
            RecurrenceType = schedule.RecurrenceType,
            RecurrenceInterval = schedule.RecurrenceInterval,
            WeeklyDays = DecodeWeeklyDays(schedule.WeeklyDaysMask),
            RecurrenceEndDate = schedule.RecurrenceEndDate
        };
    }

    private static ScheduleOccurrenceViewModel MapToOccurrenceViewModel(ScheduleItem schedule, DateTime occurrenceAt)
    {
        return new ScheduleOccurrenceViewModel
        {
            ScheduleId = schedule.Id,
            Title = schedule.Title,
            Description = schedule.Description,
            OccurrenceAt = occurrenceAt,
            DurationMinutes = schedule.DurationMinutes,
            Priority = schedule.Priority,
            PriorityLabel = GetPriorityLabel(schedule.Priority),
            RecurrenceType = schedule.RecurrenceType
        };
    }
}
