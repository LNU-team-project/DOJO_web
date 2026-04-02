using System;
using System.Collections.Generic;
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

/// <summary>
/// Тести для CalendarController та CalendarService
/// 
/// Структура тестування:
/// 1. Контролер тести - перевіряють HTTP відповіді та авторизацію (мокування сервісу)
/// 2. Сервіс тести - перевіряють бізнес-логіку (реальна БД)
/// </summary>
public class CalendarControllerTests
{
    #region Helpers

    /// <summary>
    /// Helper для створення контролера з мокованим сервісом
    /// </summary>
    private static CalendarController CreateControllerWithMockedService(Result<List<string>> serviceResult)
    {
        var mockService = new Mock<ICalendarService>();
        mockService
            .Setup(s => s.GetMarkedDatesAsync(It.IsAny<int>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(serviceResult);

        var mockLogger = Mock.Of<ILogger<CalendarController>>();
        var controller = new CalendarController(mockService.Object, mockLogger);

        // Додаємо авторизованого користувача
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

    /// <summary>
    /// Helper для створення реального сервісу з в-пам'яттю БД
    /// </summary>
    private static (CalendarService service, AppDbContext context) CreateServiceWithInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);
        var mockLogger = Mock.Of<ILogger<CalendarService>>();
        var service = new CalendarService(context, mockLogger);

        return (service, context);
    }

    #endregion

    #region Controller Tests - Авторизація та HTTP відповіді

    [Fact]
    public async Task GetMarks_ReturnsUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        var mockService = new Mock<ICalendarService>();
        var mockLogger = Mock.Of<ILogger<CalendarController>>();
        var controller = new CalendarController(mockService.Object, mockLogger)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext() // Без користувача
            }
        };

        // Act
        var result = await controller.GetMarks(
            DateOnly.FromDateTime(DateTime.UtcNow),
            DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.NotNull(unauthorized.Value);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task GetMarks_ReturnsBadRequest_WhenServiceReturnsFailure()
    {
        // Arrange
        var failureResult = Result<List<string>>.FailureResult("Невалідний діапазон дат");
        var controller = CreateControllerWithMockedService(failureResult);

        // Act
        var result = await controller.GetMarks(
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 30));

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task GetMarks_ReturnsOkWithData_WhenServiceSucceeds()
    {
        // Arrange
        var expectedDates = new List<string> { "2026-04-04", "2026-04-15", "2026-04-20" };
        var successResult = Result<List<string>>.SuccessResult(expectedDates, "Позначки отримано");
        var controller = CreateControllerWithMockedService(successResult);

        // Act
        var result = await controller.GetMarks(
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 30));

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
    }

    [Fact]
    public async Task GetMarks_ReturnsOkWithEmptyList_WhenNoMarksFound()
    {
        // Arrange
        var successResult = Result<List<string>>.SuccessResult(new List<string>(), "Позначки отримано");
        var controller = CreateControllerWithMockedService(successResult);

        // Act
        var result = await controller.GetMarks(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
    }

    #endregion

    #region Service Tests - Бізнес-логіка

    [Fact]
    public async Task CalendarService_GetMarkedDates_ReturnsFailure_WhenUserIdInvalid()
    {
        // Arrange
        var (service, _) = CreateServiceWithInMemoryDb();

        // Act
        var result = await service.GetMarkedDatesAsync(
            0, // Invalid user ID
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 30));

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Користувача не знайдено", result.Message);
    }

    [Fact]
    public async Task CalendarService_GetMarkedDates_ReturnsFailure_WhenDateRangeInvalid()
    {
        // Arrange
        var (service, _) = CreateServiceWithInMemoryDb();
        var from = new DateOnly(2026, 5, 10);
        var to = new DateOnly(2026, 5, 1);

        // Act
        var result = await service.GetMarkedDatesAsync(1, from, to);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("не може бути пізніше", result.Message);
    }

    [Fact]
    public async Task CalendarService_GetMarkedDates_ReturnsOnlyUserPlans_ExcludingOtherUsers()
    {
        // Arrange
        var (service, context) = CreateServiceWithInMemoryDb();
        
        // Додаємо плани для різних користувачів
        context.Tasks.AddRange(
            new TaskItem 
            { 
                UserId = 1, 
                Title = "Мій план 1", 
                IsPlan = true, 
                ScheduledAt = new DateTime(2026, 4, 4, 10, 0, 0, DateTimeKind.Utc) 
            },
            new TaskItem 
            { 
                UserId = 1, 
                Title = "Мій план 2", 
                IsPlan = true, 
                ScheduledAt = new DateTime(2026, 4, 15, 12, 0, 0, DateTimeKind.Utc) 
            },
            new TaskItem 
            { 
                UserId = 2, 
                Title = "План іншого користувача", 
                IsPlan = true, 
                ScheduledAt = new DateTime(2026, 4, 4, 8, 0, 0, DateTimeKind.Utc) 
            }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetMarkedDatesAsync(1, new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30));

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data!.Count);
        Assert.Equal(new[] { "2026-04-04", "2026-04-15" }, result.Data!);
    }

    [Fact]
    public async Task CalendarService_GetMarkedDates_ExcludesNonPlanTasks()
    {
        // Arrange
        var (service, context) = CreateServiceWithInMemoryDb();
        
        // Додаємо плани та звичайні завдання
        context.Tasks.AddRange(
            new TaskItem 
            { 
                UserId = 1, 
                Title = "План", 
                IsPlan = true, 
                ScheduledAt = new DateTime(2026, 4, 4, 10, 0, 0, DateTimeKind.Utc) 
            },
            new TaskItem 
            { 
                UserId = 1, 
                Title = "Звичайне завдання", 
                IsPlan = false,  // ← Це НЕ план
                ScheduledAt = new DateTime(2026, 4, 5, 10, 0, 0, DateTimeKind.Utc) 
            }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetMarkedDatesAsync(1, new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30));

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data!);
        Assert.Equal("2026-04-04", result.Data![0]);
    }

    [Fact]
    public async Task CalendarService_GetMarkedDates_ReturnsEmpty_WhenNoPlanInRange()
    {
        // Arrange
        var (service, context) = CreateServiceWithInMemoryDb();
        
        context.Tasks.Add(
            new TaskItem 
            { 
                UserId = 1, 
                Title = "План", 
                IsPlan = true, 
                ScheduledAt = new DateTime(2026, 5, 10, 10, 0, 0, DateTimeKind.Utc)  // За межами діапазону
            }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetMarkedDatesAsync(1, new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30));

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data!);
    }

    [Fact]
    public async Task CalendarService_GetMarkedDates_ReturnsDatesInAscendingOrder()
    {
        // Arrange
        var (service, context) = CreateServiceWithInMemoryDb();
        
        // Додаємо плани в беспорядку
        context.Tasks.AddRange(
            new TaskItem { UserId = 1, Title = "План 3", IsPlan = true, ScheduledAt = new DateTime(2026, 4, 20, 10, 0, 0, DateTimeKind.Utc) },
            new TaskItem { UserId = 1, Title = "План 1", IsPlan = true, ScheduledAt = new DateTime(2026, 4, 5, 10, 0, 0, DateTimeKind.Utc) },
            new TaskItem { UserId = 1, Title = "План 2", IsPlan = true, ScheduledAt = new DateTime(2026, 4, 15, 10, 0, 0, DateTimeKind.Utc) }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetMarkedDatesAsync(1, new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30));

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(new[] { "2026-04-05", "2026-04-15", "2026-04-20" }, result.Data!);
    }

    [Fact]
    public async Task CalendarService_GetMarkedDates_DeduplicatesSameDateWithMultiplePlans()
    {
        // Arrange
        var (service, context) = CreateServiceWithInMemoryDb();
        
        // Додаємо кілька планів на одну дату
        context.Tasks.AddRange(
            new TaskItem { UserId = 1, Title = "План 1", IsPlan = true, ScheduledAt = new DateTime(2026, 4, 5, 10, 0, 0, DateTimeKind.Utc) },
            new TaskItem { UserId = 1, Title = "План 2", IsPlan = true, ScheduledAt = new DateTime(2026, 4, 5, 14, 0, 0, DateTimeKind.Utc) },
            new TaskItem { UserId = 1, Title = "План 3", IsPlan = true, ScheduledAt = new DateTime(2026, 4, 5, 18, 0, 0, DateTimeKind.Utc) }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetMarkedDatesAsync(1, new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30));

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data!);
        Assert.Equal("2026-04-05", result.Data![0]);
    }

    [Fact]
    public async Task CalendarService_GetMarkedDates_HandlesSingleDayRange()
    {
        // Arrange
        var (service, context) = CreateServiceWithInMemoryDb();
        
        context.Tasks.AddRange(
            new TaskItem { UserId = 1, Title = "План", IsPlan = true, ScheduledAt = new DateTime(2026, 4, 5, 10, 0, 0, DateTimeKind.Utc) },
            new TaskItem { UserId = 1, Title = "План", IsPlan = true, ScheduledAt = new DateTime(2026, 4, 6, 10, 0, 0, DateTimeKind.Utc) }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetMarkedDatesAsync(1, new DateOnly(2026, 4, 5), new DateOnly(2026, 4, 5));

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data!);
        Assert.Equal("2026-04-05", result.Data![0]);
    }

    #endregion
}

