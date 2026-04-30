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
    private readonly IPomodoroPresetRepository _presetRepository;
    private readonly ILogger<PomodoroService> _logger;

    public PomodoroService(IAppDbContext context, IPomodoroPresetRepository presetRepository, ILogger<PomodoroService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _presetRepository = presetRepository ?? throw new ArgumentNullException(nameof(presetRepository));
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

    public async Task<Result<IReadOnlyList<PomodoroPresetViewModel>>> GetPresetsAsync(int userId)
    {
        if (userId <= 0)
        {
            return Result<IReadOnlyList<PomodoroPresetViewModel>>.FailureResult("Невалідний ідентифікатор користувача");
        }

        var presets = await _presetRepository.GetUserPresetsAsync(userId);
        var result = presets
            .Select(preset => new PomodoroPresetViewModel
            {
                Id = preset.Id,
                Name = preset.Name,
                FocusMinutes = preset.FocusMinutes,
                ShortBreakMinutes = preset.ShortBreakMinutes,
                LongBreakMinutes = preset.LongBreakMinutes,
                CreatedAt = preset.CreatedAt
            })
            .ToList();

        return Result<IReadOnlyList<PomodoroPresetViewModel>>.SuccessResult(result, "Пресети завантажено");
    }

    public async Task<Result<PomodoroPresetViewModel>> CreatePresetAsync(int userId, PomodoroPresetCreateViewModel? model)
    {
        if (userId <= 0)
        {
            return Result<PomodoroPresetViewModel>.FailureResult("Невалідний ідентифікатор користувача");
        }

        if (model == null)
        {
            return Result<PomodoroPresetViewModel>.FailureResult("Модель пресету не може бути порожньою");
        }

        var name = model.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<PomodoroPresetViewModel>.FailureResult("Назва пресету не може бути порожньою");
        }

        var normalizedName = name.ToUpperInvariant();
        var presetExists = await _presetRepository.HasPresetNameAsync(userId, normalizedName);
        if (presetExists)
        {
            return Result<PomodoroPresetViewModel>.FailureResult("Пресет з такою назвою вже існує");
        }

        var preset = new PomodoroPreset
        {
            UserId = userId,
            Name = name,
            FocusMinutes = model.FocusMinutes,
            ShortBreakMinutes = model.ShortBreakMinutes,
            LongBreakMinutes = model.LongBreakMinutes
        };

        var savedPreset = await _presetRepository.AddAsync(preset);
        _logger.LogInformation("Створено Pomodoro пресет {PresetId} для користувача {UserId}", savedPreset.Id, userId);

        return Result<PomodoroPresetViewModel>.SuccessResult(MapPreset(savedPreset), "Пресет збережено");
    }

    public async Task<Result<PomodoroPresetViewModel>> UpdatePresetAsync(int userId, int presetId, PomodoroPresetCreateViewModel? model)
    {
        if (userId <= 0 || presetId <= 0)
        {
            return Result<PomodoroPresetViewModel>.FailureResult("Невалідні дані пресету");
        }

        if (model == null)
        {
            return Result<PomodoroPresetViewModel>.FailureResult("Модель пресету не може бути порожньою");
        }

        var preset = await _presetRepository.GetUserPresetAsync(userId, presetId);
        if (preset == null)
        {
            return Result<PomodoroPresetViewModel>.FailureResult("Пресет не знайдено");
        }

        var name = model.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<PomodoroPresetViewModel>.FailureResult("Назва пресету не може бути порожньою");
        }

        var normalizedName = name.ToUpperInvariant();
        var presets = await _presetRepository.GetUserPresetsAsync(userId);
        var duplicateExists = presets.Any(existing =>
            existing.Id != presetId &&
            existing.Name.ToUpperInvariant() == normalizedName);

        if (duplicateExists)
        {
            return Result<PomodoroPresetViewModel>.FailureResult("Пресет з такою назвою вже існує");
        }

        preset.Name = name;
        preset.FocusMinutes = model.FocusMinutes;
        preset.ShortBreakMinutes = model.ShortBreakMinutes;
        preset.LongBreakMinutes = model.LongBreakMinutes;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Оновлено Pomodoro пресет {PresetId} для користувача {UserId}", presetId, userId);

        return Result<PomodoroPresetViewModel>.SuccessResult(MapPreset(preset), "Пресет оновлено");
    }

    public async Task<Result<bool>> DeletePresetAsync(int userId, int presetId)
    {
        if (userId <= 0 || presetId <= 0)
        {
            return Result<bool>.FailureResult("Невалідні дані пресету");
        }

        var preset = await _presetRepository.GetUserPresetAsync(userId, presetId);
        if (preset == null)
        {
            return Result<bool>.FailureResult("Пресет не знайдено");
        }

        await _presetRepository.DeleteAsync(preset);
        _logger.LogInformation("Pomodoro пресет {PresetId} видалено для користувача {UserId}", presetId, userId);

        return Result<bool>.SuccessResult(true, "Пресет видалено");
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

    private static PomodoroPresetViewModel MapPreset(PomodoroPreset preset)
    {
        return new PomodoroPresetViewModel
        {
            Id = preset.Id,
            Name = preset.Name,
            FocusMinutes = preset.FocusMinutes,
            ShortBreakMinutes = preset.ShortBreakMinutes,
            LongBreakMinutes = preset.LongBreakMinutes,
            CreatedAt = preset.CreatedAt
        };
    }
}
