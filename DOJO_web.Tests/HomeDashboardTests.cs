using System.IO;
using DOJO2.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace DOJO_web.Tests;

public class HomeDashboardTests
{
    private static readonly string DashboardViewPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "Presentation", "Views", "Home", "Dashboard.cshtml"));

    [Fact]
    public void Dashboard_Action_ReturnsViewResult()
    {
        var controller = new HomeController();

        var result = controller.Dashboard();

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

        Assert.Contains("~/js/dashboard.js", content, StringComparison.Ordinal);
    }
}
