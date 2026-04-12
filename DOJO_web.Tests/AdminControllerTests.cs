using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DOJO2.Controllers;
using DOJO2.Infrastructure.Results;
using DOJO2.Infrastructure.Services;
using DOJO2.Presentation.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DOJO_web.Tests;

public class AdminControllerTests
{
    private const string ModelErrorKey = "";

    private readonly Mock<IAdminService> _adminServiceMock;
    private readonly Mock<ILogger<AdminController>> _loggerMock;

    public AdminControllerTests()
    {
        _adminServiceMock = new Mock<IAdminService>();
        _loggerMock = new Mock<ILogger<AdminController>>();
    }

    [Fact]
    public async Task Users_ReturnsViewModelWithUsersAndTrimmedSearch_WhenServiceSucceeds()
    {
        var users = new List<AdminUserListItemViewModel>
        {
            new()
            {
                Id = 1,
                UserName = "vlad",
                Email = "vlad@example.com",
                Level = 4,
                ExpPoints = 250,
            }
        };

        _adminServiceMock
            .Setup(s => s.GetUsersAsync("  vlad  "))
            .ReturnsAsync(Result<List<AdminUserListItemViewModel>>.SuccessResult(users));

        var controller = CreateController();

        var actionResult = await controller.Users("  vlad  ");

        var viewResult = Assert.IsType<ViewResult>(actionResult);
        var model = Assert.IsType<AdminUsersPageViewModel>(viewResult.Model);

        Assert.Equal("vlad", model.Search);
        Assert.Single(model.Users);
        Assert.Equal("vlad@example.com", model.Users[0].Email);
        Assert.True(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Users_AddsModelErrorAndReturnsEmptyUsers_WhenServiceFails()
    {
        const string failureMessage = "Не вдалося завантажити користувачів";

        _adminServiceMock
            .Setup(s => s.GetUsersAsync(It.IsAny<string?>()))
            .ReturnsAsync(Result<List<AdminUserListItemViewModel>>.FailureResult(failureMessage));

        var controller = CreateController();

        var actionResult = await controller.Users("query");

        var viewResult = Assert.IsType<ViewResult>(actionResult);
        var model = Assert.IsType<AdminUsersPageViewModel>(viewResult.Model);

        Assert.Empty(model.Users);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(ModelErrorKey));

        var modelErrors = controller.ModelState[ModelErrorKey]?.Errors;
        Assert.NotNull(modelErrors);
        Assert.Contains(modelErrors!, error => error.ErrorMessage == failureMessage);
    }

    [Fact]
    public async Task Users_AddsDefaultModelError_WhenServiceReturnsSuccessWithoutData()
    {
        _adminServiceMock
            .Setup(s => s.GetUsersAsync(It.IsAny<string?>()))
            .ReturnsAsync(new Result<List<AdminUserListItemViewModel>>(true, null, ""));

        var controller = CreateController();

        var actionResult = await controller.Users(null);

        var viewResult = Assert.IsType<ViewResult>(actionResult);
        var model = Assert.IsType<AdminUsersPageViewModel>(viewResult.Model);

        Assert.Equal(string.Empty, model.Search);
        Assert.Empty(model.Users);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.TryGetValue(ModelErrorKey, out var entry));
        Assert.NotNull(entry);
        Assert.Contains(entry!.Errors, error => error.ErrorMessage == string.Empty);
    }

    private AdminController CreateController()
    {
        return new AdminController(_adminServiceMock.Object, _loggerMock.Object);
    }
}
