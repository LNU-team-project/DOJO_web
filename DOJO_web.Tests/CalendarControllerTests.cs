using System;
using System.Security.Claims;
using System.Threading.Tasks;
using DOJO2.Controllers;
using DOJO2.Domain.Entities;
using DOJO2.Infrastructure.Data;
using DOJO2.Infrastructure.Results;
using DOJO2.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DOJO_web.Tests;

public class CalendarControllerTests
{
    private static CalendarController CreateController(Result<System.Collections.Generic.List<string>> serviceResult)
    {
        var service = new Mock<ICalendarService>();
        service
            .Setup(s => s.GetMarkedDatesAsync(It.IsAny<int>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(serviceResult);

        var logger = Mock.Of<ILogger<CalendarController>>();
        var controller = new CalendarController(service.Object, logger);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1")
        }, "mock"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        return controller;
    }

    private static CalendarService CreateService(AppDbContext context)
    {
        var logger = Mock.Of<ILogger<CalendarService>>();
        return new CalendarService(context, logger);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetMarks_ReturnsUnauthorized_WhenNoUser()
    {
        var service = new Mock<ICalendarService>();
        var logger = Mock.Of<ILogger<CalendarController>>();
        var controller = new CalendarController(service.Object, logger)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext() // no user
            }
        };

        var result = await controller.GetMarks(DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow));

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var payload = unauthorized.Value;
        Assert.True(payload is not null);
        Assert.False((bool)payload.GetType().GetProperty("success")!.GetValue(payload)!);
    }

    [Fact]
    public async Task GetMarks_ReturnsBadRequest_OnFailure()
    {
        var failure = Result<System.Collections.Generic.List<string>>.FailureResult("err");
        var controller = CreateController(failure);

        var result = await controller.GetMarks(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30));

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var payload = bad.Value;
        Assert.True(payload is not null);
        Assert.False((bool)payload.GetType().GetProperty("success")!.GetValue(payload)!);
        Assert.Equal("err", (string)payload.GetType().GetProperty("message")!.GetValue(payload)!);
    }

    [Fact]
    public async Task GetMarks_ReturnsOk_OnSuccess()
    {
        var success = Result<System.Collections.Generic.List<string>>.SuccessResult(new System.Collections.Generic.List<string> { "2026-04-04" }, "ok");
        var controller = CreateController(success);

        var result = await controller.GetMarks(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30));

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = ok.Value;
        Assert.True(payload is not null);
        Assert.True((bool)payload.GetType().GetProperty("success")!.GetValue(payload)!);
        Assert.Equal("ok", (string)payload.GetType().GetProperty("message")!.GetValue(payload)!);
        var data = payload.GetType().GetProperty("data")!.GetValue(payload) as System.Collections.Generic.IEnumerable<string>;
        Assert.NotNull(data);
        Assert.Single(data!);
    }

    [Fact]
    public async Task CalendarService_GetMarkedDates_ReturnsFailure_WhenUserInvalid()
    {
        using var ctx = CreateContext();
        var service = CreateService(ctx);

        var result = await service.GetMarkedDatesAsync(0, DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow));

        Assert.False(result.Success);
        Assert.Contains("Користувача не знайдено", result.Message);
    }

    [Fact]
    public async Task CalendarService_GetMarkedDates_ReturnsFailure_WhenRangeInvalid()
    {
        using var ctx = CreateContext();
        var service = CreateService(ctx);

        var from = new DateOnly(2026, 5, 10);
        var to = new DateOnly(2026, 5, 1);

        var result = await service.GetMarkedDatesAsync(1, from, to);

        Assert.False(result.Success);
        Assert.Contains("не може бути пізніше", result.Message);
    }

    [Fact]
    public async Task CalendarService_GetMarkedDates_ReturnsMarks_ForUserAndRange()
    {
        using var ctx = CreateContext();
        var service = CreateService(ctx);

        ctx.Tasks.AddRange(
            new TaskItem { UserId = 1, Title = "a", IsPlan = true, ScheduledAt = new DateTime(2026, 4, 4, 10, 0, 0, DateTimeKind.Utc) },
            new TaskItem { UserId = 1, Title = "b", IsPlan = true, ScheduledAt = new DateTime(2026, 4, 15, 12, 0, 0, DateTimeKind.Utc) },
            new TaskItem { UserId = 2, Title = "c", IsPlan = true, ScheduledAt = new DateTime(2026, 4, 4, 8, 0, 0, DateTimeKind.Utc) },
            new TaskItem { UserId = 1, Title = "d", IsPlan = false, ScheduledAt = new DateTime(2026, 4, 20, 8, 0, 0, DateTimeKind.Utc) }
        );
        await ctx.SaveChangesAsync();

        var result = await service.GetMarkedDatesAsync(1, new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30));

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(new[] { "2026-04-04", "2026-04-15" }, result.Data);
    }

    [Fact]
    public async Task CalendarService_GetMarkedDates_ReturnsEmpty_WhenNoMatches()
    {
        using var ctx = CreateContext();
        var service = CreateService(ctx);

        var result = await service.GetMarkedDatesAsync(1, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data);
    }
}
