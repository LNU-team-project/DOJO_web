using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using DOJO2.Controllers;
using DOJO2.Domain.Entities;
using DOJO2.Presentation.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace DOJO_web.Tests;

public class AccountControllerTests
{
    private readonly Mock<UserManager<AppUser>> _mockUserManager;
    private readonly Mock<SignInManager<AppUser>> _mockSignInManager;
    private readonly Mock<ILogger<AccountController>> _mockLogger;
    private readonly Mock<IEmailSender> _mockEmailSender;
    private readonly AccountController _controller;

    public AccountControllerTests()
    {
        var options = Mock.Of<IOptions<IdentityOptions>>();
        var passwordHasher = Mock.Of<IPasswordHasher<AppUser>>();
        var userValidators = new List<IUserValidator<AppUser>>();
        var passwordValidators = new List<IPasswordValidator<AppUser>>();
        var keyNormalizer = Mock.Of<ILookupNormalizer>();
        var errors = new IdentityErrorDescriber();
        var services = Mock.Of<IServiceProvider>();
        var userManagerLogger = Mock.Of<ILogger<UserManager<AppUser>>>();

        _mockUserManager = new Mock<UserManager<AppUser>>(
            Mock.Of<IUserStore<AppUser>>(),
            options,
            passwordHasher,
            userValidators,
            passwordValidators,
            keyNormalizer,
            errors,
            services,
            userManagerLogger);

        var contextAccessor = Mock.Of<IHttpContextAccessor>();
        var claimsFactory = Mock.Of<IUserClaimsPrincipalFactory<AppUser>>();
        var schemeProvider = Mock.Of<IAuthenticationSchemeProvider>();
        var signInManagerLogger = Mock.Of<ILogger<SignInManager<AppUser>>>();
        var userConfirmation = Mock.Of<IUserConfirmation<AppUser>>();
        
        _mockSignInManager = new Mock<SignInManager<AppUser>>(
            _mockUserManager.Object,
            contextAccessor,
            claimsFactory,
            options,
            signInManagerLogger,
            schemeProvider,
            userConfirmation);

        _mockLogger = new Mock<ILogger<AccountController>>();

        _mockEmailSender = new Mock<IEmailSender>();

        _controller = new AccountController(
            _mockUserManager.Object,
            _mockSignInManager.Object,
            _mockLogger.Object,
            _mockEmailSender.Object);
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
        var model = new LoginViewModel();

        // Act
        var result = await _controller.Login(model);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(model, viewResult.Model);
    }

    [Fact]
    public async Task Login_Post_ReturnsViewWithError_WhenUserNotFound()
    {
        // Arrange
        var model = new LoginViewModel { Email = "test@example.com", Password = "password" };
        _mockUserManager.Setup(um => um.FindByEmailAsync(model.Email)).ReturnsAsync((AppUser?)null);

        // Act
        var result = await _controller.Login(model);

        // Assert
        Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
        Assert.Contains(_controller.ModelState.Values, v => v.Errors.Any(e => e.ErrorMessage == "Невірна пошта або пароль."));
    }

    [Fact]
    public async Task Login_Post_ReturnsViewWithError_WhenPasswordIsIncorrect()
    {
        // Arrange
        var user = new AppUser { UserName = "testuser", Email = "test@example.com" };
        var model = new LoginViewModel { Email = user.Email, Password = "wrongpassword" };
        _mockUserManager.Setup(um => um.FindByEmailAsync(model.Email)).ReturnsAsync(user);
        _mockSignInManager.Setup(sm => sm.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        // Act
        var result = await _controller.Login(model);

        // Assert
        Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
        Assert.Contains(_controller.ModelState.Values, v => v.Errors.Any(e => e.ErrorMessage == "Невірна пошта або пароль."));
    }

    [Fact]
    public async Task Login_Post_RedirectsToDashboard_WhenCredentialsAreCorrect()
    {
        // Arrange
        var user = new AppUser { UserName = "testuser", Email = "test@example.com" };
        var model = new LoginViewModel { Email = user.Email, Password = "password123" };
        _mockUserManager.Setup(um => um.FindByEmailAsync(model.Email)).ReturnsAsync(user);
        _mockSignInManager.Setup(sm => sm.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        // Act
        var result = await _controller.Login(model);

        // Assert
        var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Dashboard", redirectToActionResult.ActionName);
        Assert.Equal("Home", redirectToActionResult.ControllerName);
    }
}