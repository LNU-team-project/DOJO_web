using DOJO2.Presentation.Controllers;
using DOJO2.Application.Interfaces;
using DOJO2.Application.Common;
using DOJO2.Application.ViewModels;
using DOJO2.Domain.Entities;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace DOJO_web.Tests;

public class HomeDashboardTests
{
    private static readonly string DashboardViewPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "Presentation", "Views", "Home", "Dashboard.cshtml"));

    [Fact]
    public async Task Dashboard_Action_ReturnsViewResult()
    {
        var statisticsServiceMock = new Mock<IStatisticsService>();
        statisticsServiceMock
            .Setup(s => s.GetTodayStatisticsAsync(It.IsAny<int>(), It.IsAny<DateTime>()))
            .ReturnsAsync(Result<StatisticsViewModel>.SuccessResult(new StatisticsViewModel(), "ok"));

        var loggerMock = new Mock<ILogger<HomeController>>();

        var controller = new HomeController(statisticsServiceMock.Object, loggerMock.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "1") },
            "TestAuth"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var result = await controller.Dashboard();

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Dashboard_View_ContainsWeekNavigationButtons()
    {
        var content = File.ReadAllText(DashboardViewPath);

        Assert.Contains("data-range-dir=\"prev\"", content, StringComparison.Ordinal);
        Assert.Contains("data-range-dir=\"next\"", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Dashboard_View_ContainsDateRangeLabel()
    {
        var content = File.ReadAllText(DashboardViewPath);

        Assert.Contains("data-range-label", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Dashboard_View_ContainsCalendarGridContainers()
    {
        var content = File.ReadAllText(DashboardViewPath);

        Assert.Contains("data-days-header", content, StringComparison.Ordinal);
        Assert.Contains("data-time-grid", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Dashboard_View_ReferencesDashboardScript()
    {
        var content = File.ReadAllText(DashboardViewPath);

        Assert.Contains("~/js/dashboard.bundle.min.js", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Dashboard_View_ContainsNotificationsEndpoint()
    {
        var content = File.ReadAllText(DashboardViewPath);

        Assert.Contains("data-notifications-url", content, StringComparison.Ordinal);
        Assert.Contains("~/api/notifications/dashboard", content, StringComparison.Ordinal);
    }
}