using System.Security.Claims;
using System.Threading.Tasks;
using DOJO2.Controllers;
using DOJO2.Application.Interfaces;
using DOJO2.Domain.Entities;
using DOJO2.Application.Common;
using DOJO2.Infrastructure.Services;
using DOJO2.Application.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DOJO_web.Tests;

public class AccountControllerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly Mock<ILogger<AccountController>> _mockLogger;
    private readonly AccountController _controller;

    public AccountControllerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _mockLogger = new Mock<ILogger<AccountController>>();

        _controller = new AccountController(
            _mockAuthService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void Login_ReturnsViewResult_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = _controller.Login();

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Login_SetsBlockedMessage_WhenBlockedFlagIsTrue()
    {
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        var result = _controller.Login(blocked: true);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.NotNull(viewResult.Model);
        Assert.Equal("Ваш обліковий запис заблоковано. Зверніться до адміністратора.", _controller.ViewData["BlockedMessage"]);
    }

    [Fact]
    public void Login_RedirectsToDashboard_WhenUserIsAuthenticated()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") }, "mock"));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = _controller.Login();

        // Assert
        var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Dashboard", redirectToActionResult.ActionName);
        Assert.Equal("Home", redirectToActionResult.ControllerName);
    }

    [Fact]
    public async Task Login_Post_ReturnsViewWithModel_WhenModelStateIsInvalid()
    {
        // Arrange
        _controller.ModelState.AddModelError("Email", "Required");
        var model = new LoginViewModel { Email = "", Password = "" };

        // Act
        var result = await _controller.Login(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(model, viewResult.Model);
    }

    [Fact]
    public async Task Login_Post_ReturnsViewWithError_WhenLoginFails()
    {
        // Arrange
        var model = new LoginViewModel { Email = "test@example.com", Password = "password" };
        _mockAuthService.Setup(s => s.LoginAsync(model.Email, model.Password, false))
            .ReturnsAsync(Result<bool>.FailureResult("Невірна пошта або пароль."));

        // Act
        var result = await _controller.Login(model);

        // Assert
        Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Login_Post_ReturnsRedirectToDashboard_WhenLoginSucceeds()
    {
        // Arrange
        var model = new LoginViewModel { Email = "test@example.com", Password = "password" };
        _mockAuthService.Setup(s => s.LoginAsync(model.Email, model.Password, false))
            .ReturnsAsync(Result<bool>.SuccessResult(true, "Успішний вхід"));

        // Act
        var result = await _controller.Login(model);

        // Assert
        var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Dashboard", redirectToActionResult.ActionName);
        Assert.Equal("Home", redirectToActionResult.ControllerName);
    }

    [Fact]
    public async Task Register_Post_ReturnsViewWithModel_WhenModelStateIsInvalid()
    {
        // Arrange
        _controller.ModelState.AddModelError("Email", "Required");
        var model = new RegisterViewModel { UserName = "", Email = "", Password = "" };

        // Act
        var result = await _controller.Register(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(model, viewResult.Model);
    }

    [Fact]
    public async Task Register_Post_ReturnsViewWithError_WhenRegistrationFails()
    {
        // Arrange
        var model = new RegisterViewModel { UserName = "testuser", Email = "test@example.com", Password = "password" };
        _mockAuthService.Setup(s => s.RegisterAsync(model.UserName, model.Email, model.Password))
            .ReturnsAsync(Result<bool>.FailureResult("Email вже використовується", new List<string> { "Email exists" }));

        // Act
        var result = await _controller.Register(model);

        // Assert
        Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Register_Post_ReturnsRedirectToDashboard_WhenRegistrationSucceeds()
    {
        // Arrange
        var model = new RegisterViewModel { UserName = "testuser", Email = "test@example.com", Password = "password" };
        _mockAuthService.Setup(s => s.RegisterAsync(model.UserName, model.Email, model.Password))
            .ReturnsAsync(Result<bool>.SuccessResult(true, "Успішна реєстрація"));

        // Act
        var result = await _controller.Register(model);

        // Assert
        var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Dashboard", redirectToActionResult.ActionName);
        Assert.Equal("Home", redirectToActionResult.ControllerName);
    }
}

