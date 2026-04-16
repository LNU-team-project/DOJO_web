using DOJO2.Domain.Entities;
using DOJO2.Infrastructure.Data;
using DOJO2.Application.Common;
using DOJO2.Application.Interfaces;
using DOJO2.Application.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace DOJO2.Infrastructure.Services;

public class PomodoroService : IPomodoroService
{
    private readonly IAppDbContext _context;
    private readonly ILogger<PomodoroService> _logger;

    public PomodoroService(IAppDbContext context, ILogger<PomodoroService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<PomodoroTodayStatsViewModel>> GetTodayStatsAsync(int userId, DateTime utcNow)
    {
        var dayStartUtc = DateTime.SpecifyKind(utcNow.Date, DateTimeKind.Utc);
        var dayEndUtc = dayStartUtc.AddDays(1);

        var stats = await _context.Pomodoros
            .Where(p => p.UserId == userId && p.StartTime >= dayStartUtc && p.StartTime < dayEndUtc)
            .GroupBy(_ => 1)
            .Select(group => new PomodoroTodayStatsViewModel
            {
                CompletedFocusSessions = group.Count(),
                TotalFocusMinutes = group.Sum(item => (int?)item.DurationMinutes) ?? 0
            })
            .SingleOrDefaultAsync();

        return Result<PomodoroTodayStatsViewModel>.SuccessResult(
            stats ?? new PomodoroTodayStatsViewModel(),
            "Статистику Pomodoro отримано"
        );
    }

    public async Task<Result<PomodoroTodayStatsViewModel>> CreateSessionAsync(int userId, PomodoroSessionCreateViewModel? model)
    {
        if (model == null)
        {
            return Result<PomodoroTodayStatsViewModel>.FailureResult("Модель Pomodoro не може бути порожньою");
        }

        if (model.DurationMinutes <= 0)
        {
            return Result<PomodoroTodayStatsViewModel>.FailureResult("Тривалість Pomodoro має бути більшою за 0");
        }

        if (model.WorkCycles <= 0)
        {
            return Result<PomodoroTodayStatsViewModel>.FailureResult("Кількість циклів має бути більшою за 0");
        }

        var startUtc = NormalizeUtc(model.StartTime);
        var endUtc = NormalizeUtc(model.EndTime);

        if (endUtc <= startUtc)
        {
            return Result<PomodoroTodayStatsViewModel>.FailureResult("Час завершення має бути пізнішим за час початку");
        }

        if (model.TaskId.HasValue)
        {
            var taskExists = await _context.Tasks
                .AnyAsync(task => task.Id == model.TaskId.Value && task.UserId == userId);

            if (!taskExists)
            {
                return Result<PomodoroTodayStatsViewModel>.FailureResult("Пов'язане завдання не знайдено");
            }
        }

        var pomodoro = new Pomodoro
        {
            UserId = userId,
            TaskId = model.TaskId,
            StartTime = startUtc,
            EndTime = endUtc,
            DurationMinutes = model.DurationMinutes,
            WorkCycles = model.WorkCycles
        };

        _context.Pomodoros.Add(pomodoro);
        await _context.SaveChangesAsync();

        _logger.LogDebug("Pomodoro сесію {PomodoroId} збережено для користувача {UserId}", pomodoro.Id, userId);

        return await GetTodayStatsAsync(userId, DateTime.UtcNow);
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
