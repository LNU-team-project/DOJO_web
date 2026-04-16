using System.Collections.Generic;
using System.Threading.Tasks;
using DOJO2.Controllers;
using DOJO2.Application.Interfaces;
using DOJO2.Infrastructure.Results;
using DOJO2.Infrastructure.Services;
using DOJO2.Application.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DOJO_web.Tests;

public class AdminControllerTests
{
    private const string ModelErrorKey = "";
    private const string SuccessMessageTempDataKey = "AdminUsersSuccessMessage";
    private const string ErrorMessageTempDataKey = "AdminUsersErrorMessage";

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


//TEST BLOCK/UNBLOCK
    [Fact]
    public async Task BlockUser_RedirectsAndWritesSuccessTempData_WhenServiceSucceeds()
    {
        _adminServiceMock
            .Setup(s => s.BlockUserAsync(7))
            .ReturnsAsync(Result<bool>.SuccessResult(true, "Користувача успішно заблоковано"));

        var controller = CreateController();

        var actionResult = await controller.BlockUser(7, "vlad");

        var redirectResult = Assert.IsType<RedirectToActionResult>(actionResult);
        Assert.Equal("Users", redirectResult.ActionName);
        Assert.Equal("vlad", redirectResult.RouteValues?["search"]);
        Assert.Equal("Користувача успішно заблоковано", controller.TempData[SuccessMessageTempDataKey]);
        Assert.Null(controller.TempData[ErrorMessageTempDataKey]);
    }

    [Fact]
    public async Task BlockUser_RedirectsAndWritesErrorTempData_WhenServiceFails()
    {
        _adminServiceMock
            .Setup(s => s.BlockUserAsync(7))
            .ReturnsAsync(Result<bool>.FailureResult("Не вдалося заблокувати користувача"));

        var controller = CreateController();

        var actionResult = await controller.BlockUser(7, "vlad");

        var redirectResult = Assert.IsType<RedirectToActionResult>(actionResult);
        Assert.Equal("Users", redirectResult.ActionName);
        Assert.Equal("vlad", redirectResult.RouteValues?["search"]);
        Assert.Equal("Не вдалося заблокувати користувача", controller.TempData[ErrorMessageTempDataKey]);
        Assert.Null(controller.TempData[SuccessMessageTempDataKey]);
    }

    [Fact]
    public async Task UnblockUser_RedirectsAndWritesSuccessTempData_WhenServiceSucceeds()
    {
        _adminServiceMock
            .Setup(s => s.UnblockUserAsync(9))
            .ReturnsAsync(Result<bool>.SuccessResult(true, "Користувача успішно розблоковано"));

        var controller = CreateController();

        var actionResult = await controller.UnblockUser(9, "mail");

        var redirectResult = Assert.IsType<RedirectToActionResult>(actionResult);
        Assert.Equal("Users", redirectResult.ActionName);
        Assert.Equal("mail", redirectResult.RouteValues?["search"]);
        Assert.Equal("Користувача успішно розблоковано", controller.TempData[SuccessMessageTempDataKey]);
        Assert.Null(controller.TempData[ErrorMessageTempDataKey]);
    }

    [Fact]
    public async Task UnblockUser_RedirectsAndWritesErrorTempData_WhenServiceFails()
    {
        _adminServiceMock
            .Setup(s => s.UnblockUserAsync(9))
            .ReturnsAsync(Result<bool>.FailureResult("Не вдалося розблокувати користувача"));

        var controller = CreateController();

        var actionResult = await controller.UnblockUser(9, "mail");

        var redirectResult = Assert.IsType<RedirectToActionResult>(actionResult);
        Assert.Equal("Users", redirectResult.ActionName);
        Assert.Equal("mail", redirectResult.RouteValues?["search"]);
        Assert.Equal("Не вдалося розблокувати користувача", controller.TempData[ErrorMessageTempDataKey]);
        Assert.Null(controller.TempData[SuccessMessageTempDataKey]);
    }

    [Fact]
    public async Task DeleteUser_RedirectsAndWritesSuccessTempData_WhenServiceSucceeds()
    {
        _adminServiceMock
            .Setup(s => s.DeleteUserAsync(5))
            .ReturnsAsync(Result<bool>.SuccessResult(true, "Користувача успішно видалено"));

        var controller = CreateController();

        var actionResult = await controller.DeleteUser(5, "vlad");

        var redirectResult = Assert.IsType<RedirectToActionResult>(actionResult);
        Assert.Equal("Users", redirectResult.ActionName);
        Assert.Equal("vlad", redirectResult.RouteValues?["search"]);
        Assert.Equal("Користувача успішно видалено", controller.TempData[SuccessMessageTempDataKey]);
        Assert.Null(controller.TempData[ErrorMessageTempDataKey]);
    }

    [Fact]
    public async Task DeleteUser_RedirectsAndWritesErrorTempData_WhenServiceFails()
    {
        _adminServiceMock
            .Setup(s => s.DeleteUserAsync(5))
            .ReturnsAsync(Result<bool>.FailureResult("Не вдалося видалити користувача"));

        var controller = CreateController();

        var actionResult = await controller.DeleteUser(5, "vlad");

        var redirectResult = Assert.IsType<RedirectToActionResult>(actionResult);
        Assert.Equal("Users", redirectResult.ActionName);
        Assert.Equal("vlad", redirectResult.RouteValues?["search"]);
        Assert.Equal("Не вдалося видалити користувача", controller.TempData[ErrorMessageTempDataKey]);
        Assert.Null(controller.TempData[SuccessMessageTempDataKey]);
    }

    private AdminController CreateController()
    {
        var controller = new AdminController(_adminServiceMock.Object, _loggerMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var tempDataProvider = new Mock<ITempDataProvider>();
        controller.TempData = new TempDataDictionary(controller.HttpContext, tempDataProvider.Object);

        return controller;
    }
}
