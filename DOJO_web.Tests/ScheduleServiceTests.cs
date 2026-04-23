using DOJO2.Application.ViewModels;
using DOJO2.Domain.Entities;
using DOJO2.Infrastructure.Data;
using DOJO2.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace DOJO_web.Tests;

public class ScheduleServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateScheduleAsync_WithNullModel_ReturnsFailure()
    {
        using var context = CreateContext();
        var service = new ScheduleService(context);

        var result = await service.CreateScheduleAsync(1, null);

        Assert.False(result.Success);
        Assert.Equal("Модель розкладу не може бути порожньою", result.Message);
    }

    [Fact]
    public async Task CreateScheduleAsync_WithInvalidDuration_ReturnsFailure()
    {
        using var context = CreateContext();
        var service = new ScheduleService(context);

        var result = await service.CreateScheduleAsync(2, new ScheduleCreateViewModel
        {
            Title = "Deep Work",
            StartAt = new DateTime(2026, 4, 23, 9, 0, 0, DateTimeKind.Utc),
            DurationMinutes = 4,
            RecurrenceType = "daily",
            RecurrenceInterval = 1
        });

        Assert.False(result.Success);
        Assert.Equal("Тривалість має бути від 5 до 720 хвилин", result.Message);
    }

    [Fact]
    public async Task CreateScheduleAsync_WithInvalidRecurrenceType_ReturnsFailure()
    {
        using var context = CreateContext();
        var service = new ScheduleService(context);

        var result = await service.CreateScheduleAsync(3, new ScheduleCreateViewModel
        {
            Title = "Training",
            StartAt = new DateTime(2026, 4, 23, 7, 30, 0, DateTimeKind.Utc),
            DurationMinutes = 45,
            RecurrenceType = "yearly",
            RecurrenceInterval = 1
        });

        Assert.False(result.Success);
        Assert.Equal("Недопустимий тип повторення", result.Message);
    }

    [Fact]
    public async Task CreateScheduleAsync_WithInvalidRecurrenceInterval_ReturnsFailure()
    {
        using var context = CreateContext();
        var service = new ScheduleService(context);

        var result = await service.CreateScheduleAsync(4, new ScheduleCreateViewModel
        {
            Title = "English",
            StartAt = new DateTime(2026, 4, 23, 18, 0, 0, DateTimeKind.Utc),
            DurationMinutes = 60,
            RecurrenceType = "weekly",
            RecurrenceInterval = 0
        });

        Assert.False(result.Success);
        Assert.Equal("Інтервал повторення має бути від 1 до 30", result.Message);
    }

    [Fact]
    public async Task CreateScheduleAsync_DailySchedule_CreatesAndReturnsMappedData()
    {
        using var context = CreateContext();
        var service = new ScheduleService(context);

        var startAt = new DateTime(2026, 4, 23, 10, 15, 0, DateTimeKind.Unspecified);
        var result = await service.CreateScheduleAsync(5, new ScheduleCreateViewModel
        {
            Title = "  Daily Focus  ",
            Description = "  Work on important task  ",
            StartAt = startAt,
            DurationMinutes = 50,
            Priority = 3,
            RecurrenceType = "DAILY",
            RecurrenceInterval = 1
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("Розклад створено", result.Message);
        Assert.Equal("Daily Focus", result.Data!.Title);
        Assert.Equal("Work on important task", result.Data.Description);
        Assert.Equal(DateTimeKind.Utc, result.Data.StartAt.Kind);
        Assert.Equal("daily", result.Data.RecurrenceType);
        Assert.Equal("Висока", result.Data.PriorityLabel);
    }

    [Fact]
    public async Task CreateScheduleAsync_WeeklyWithoutDays_UsesStartDayAsWeeklyMask()
    {
        using var context = CreateContext();
        var service = new ScheduleService(context);

        var startAt = new DateTime(2026, 4, 27, 8, 0, 0, DateTimeKind.Utc); // Monday
        var result = await service.CreateScheduleAsync(6, new ScheduleCreateViewModel
        {
            Title = "Sprint Planning",
            StartAt = startAt,
            DurationMinutes = 60,
            RecurrenceType = "weekly",
            RecurrenceInterval = 1,
            WeeklyDays = new List<int>()
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data!.WeeklyDays);
        Assert.Equal((int)DayOfWeek.Monday, result.Data.WeeklyDays[0]);
    }

    [Fact]
    public async Task CreateScheduleAsync_WeeklyWithSelectedDays_PersistsAllSelectedDays()
    {
        using var context = CreateContext();
        var service = new ScheduleService(context);

        var result = await service.CreateScheduleAsync(7, new ScheduleCreateViewModel
        {
            Title = "Gym",
            StartAt = new DateTime(2026, 4, 23, 19, 0, 0, DateTimeKind.Utc),
            DurationMinutes = 90,
            RecurrenceType = "weekly",
            RecurrenceInterval = 1,
            WeeklyDays = new List<int> { 1, 3, 5 }
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("weekly", result.Data!.RecurrenceType);
        Assert.Equal(new List<int> { 1, 3, 5 }, result.Data.WeeklyDays);
    }

    [Fact]
    public async Task GetSchedulesForRangeAsync_WithNoEndDate_ReturnsOccurrencesInFarFuture()
    {
        using var context = CreateContext();
        var service = new ScheduleService(context);

        var startAt = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var createResult = await service.CreateScheduleAsync(7, new ScheduleCreateViewModel
        {
            Title = "Standup",
            StartAt = startAt,
            RecurrenceType = "weekly",
            RecurrenceInterval = 1,
            WeeklyDays = new List<int> { 1 },
            RecurrenceEndDate = null
        });

        Assert.True(createResult.Success);

        var rangeStart = new DateTime(2026, 12, 7, 0, 0, 0, DateTimeKind.Utc);
        var rangeEnd = new DateTime(2026, 12, 13, 23, 59, 0, DateTimeKind.Utc);

        var result = await service.GetSchedulesForRangeAsync(7, rangeStart, rangeEnd);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data!);
        Assert.Equal("Standup", result.Data[0].Title);
    }

    [Fact]
    // Нові тести
    public async Task DeleteScheduleOccurrenceAsync_SingleMode_DeletesOnlyOneOccurrence()
    {
        using var context = CreateContext();
        var service = new ScheduleService(context);

        var startAt = new DateTime(2026, 4, 20, 10, 0, 0, DateTimeKind.Utc);
        var createResult = await service.CreateScheduleAsync(11, new ScheduleCreateViewModel
        {
            Title = "Workout",
            StartAt = startAt,
            RecurrenceType = "daily",
            RecurrenceInterval = 1
        });

        Assert.True(createResult.Success);

        var deleteTarget = startAt.AddDays(1);
        var deleteResult = await service.DeleteScheduleOccurrenceAsync(11, new ScheduleDeleteViewModel
        {
            ScheduleId = createResult.Data!.Id,
            OccurrenceAt = deleteTarget,
            DeleteMode = "single"
        });

        Assert.True(deleteResult.Success);

        var rangeResult = await service.GetSchedulesForRangeAsync(
            11,
            startAt,
            startAt.AddDays(2).AddHours(23));

        Assert.True(rangeResult.Success);
        Assert.NotNull(rangeResult.Data);
        Assert.Equal(2, rangeResult.Data!.Count);
        Assert.DoesNotContain(rangeResult.Data, item => item.OccurrenceAt == deleteTarget);
    }

    [Fact]
    // Нові тести
    public async Task DeleteScheduleOccurrenceAsync_FutureMode_DeletesSelectedAndNextOccurrences()
    {
        using var context = CreateContext();
        var service = new ScheduleService(context);

        var startAt = new DateTime(2026, 4, 20, 8, 30, 0, DateTimeKind.Utc);
        var createResult = await service.CreateScheduleAsync(15, new ScheduleCreateViewModel
        {
            Title = "Reading",
            StartAt = startAt,
            RecurrenceType = "daily",
            RecurrenceInterval = 1
        });

        Assert.True(createResult.Success);

        var deleteFrom = startAt.AddDays(2);
        var deleteResult = await service.DeleteScheduleOccurrenceAsync(15, new ScheduleDeleteViewModel
        {
            ScheduleId = createResult.Data!.Id,
            OccurrenceAt = deleteFrom,
            DeleteMode = "future"
        });

        Assert.True(deleteResult.Success);

        var rangeResult = await service.GetSchedulesForRangeAsync(
            15,
            startAt,
            startAt.AddDays(5));

        Assert.True(rangeResult.Success);
        Assert.NotNull(rangeResult.Data);
        Assert.Equal(2, rangeResult.Data!.Count);
        Assert.All(rangeResult.Data, item => Assert.True(item.OccurrenceAt < deleteFrom));
    }
}
