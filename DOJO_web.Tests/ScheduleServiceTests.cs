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
