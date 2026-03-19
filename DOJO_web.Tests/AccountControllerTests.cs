using DOJO2.Controllers;
using DOJO2.Domain.Entities;
using DOJO2.Presentation.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using Xunit;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

public class AccountControllerTests
{
    [Fact]
    public async Task Register_WhenModelStateInvalid_ReturnsViewWithModel()
    {
        var controller = BuildController();
        controller.ModelState.AddModelError("Email", "Invalid email");

        var model = new RegisterViewModel
        {
            UserName = "testuser",
            Email = "bad",
            Password = "secret1",
            ConfirmPassword = "secret1"
        };

        var result = await controller.Register(model);

        var view = Assert.IsType<ViewResult>(result);
        var returnedModel = Assert.IsType<RegisterViewModel>(view.Model);
        Assert.Equal(model.Email, returnedModel.Email);
    }

    [Fact]
    public async Task Register_WhenCreateSucceeds_RedirectsToHomeIndex()
    {
        var userManager = BuildUserManager();
        userManager
            .Setup(m => m.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var controller = BuildController(userManager: userManager);

        var model = new RegisterViewModel
        {
            UserName = "testuser",
            Email = "user@example.com",
            Password = "secret1",
            ConfirmPassword = "secret1"
        };

        var result = await controller.Register(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public void Login_Get_WhenAuthenticated_RedirectsToHomeIndex()
    {
        var controller = BuildController();
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "user") }, authenticationType: "mock");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        var result = controller.Login();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public void Login_Get_WhenAnonymous_ReturnsViewWithReturnUrl()
    {
        var controller = BuildController();
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = controller.Login(returnUrl: "/tasks");

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<LoginViewModel>(view.Model);
        Assert.Equal("/tasks", controller.ViewData["ReturnUrl"]);
    }

    [Fact]
    public async Task Login_Post_WhenModelStateInvalid_ReturnsView()
    {
        var controller = BuildController();
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.ModelState.AddModelError("Email", "Required");

        var model = new LoginViewModel { Email = "user@example.com", Password = string.Empty };

        var result = await controller.Login(model);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(model, view.Model);
    }

    [Fact]
    public async Task Login_Post_WhenUserNotFound_AddsErrorAndReturnsView()
    {
        var userManager = BuildUserManager();
        userManager
            .Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((AppUser?)null);

        var controller = BuildController(userManager: userManager);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var model = new LoginViewModel { Email = "missing@example.com", Password = "secret", RememberMe = false };

        var result = await controller.Login(model);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(model, view.Model);
        Assert.Contains(controller.ModelState[string.Empty]?.Errors ?? [], e => e.ErrorMessage.Contains("Невірна пошта"));
    }

    [Fact]
    public async Task Login_Post_WhenPasswordSignInFails_AddsErrorAndReturnsView()
    {
        var userManager = BuildUserManager();
        userManager
            .Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new AppUser { UserName = "user", Email = "user@example.com" });

        var signInManager = BuildSignInManager(userManager.Object);
        signInManager
            .Setup(m => m.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), true))
            .ReturnsAsync(SignInResult.Failed);

        var controller = BuildController(userManager: userManager, signInManager: signInManager);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var model = new LoginViewModel { Email = "user@example.com", Password = "wrong" };

        var result = await controller.Login(model);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(model, view.Model);
        Assert.Contains(controller.ModelState[string.Empty]?.Errors ?? [], e => e.ErrorMessage.Contains("Невірна пошта"));
    }

    [Fact]
    public async Task Login_Post_WhenPasswordSignInSucceeds_RedirectsToHomeIndex()
    {
        var userManager = BuildUserManager();
        userManager
            .Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new AppUser { UserName = "user", Email = "user@example.com" });

        var signInManager = BuildSignInManager(userManager.Object);
        signInManager
            .Setup(m => m.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), true))
            .ReturnsAsync(SignInResult.Success);

        var controller = BuildController(userManager: userManager, signInManager: signInManager);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var model = new LoginViewModel { Email = "user@example.com", Password = "secret" };

        var result = await controller.Login(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    private static AccountController BuildController(Mock<UserManager<AppUser>>? userManager = null)
    {
        userManager ??= BuildUserManager();
        var signInManager = BuildSignInManager(userManager.Object);
        return BuildController(userManager, signInManager);
    }

    private static AccountController BuildController(Mock<UserManager<AppUser>> userManager, Mock<SignInManager<AppUser>> signInManager)
    {
        var logger = new Mock<ILogger<AccountController>>();

        return new AccountController(userManager.Object, signInManager.Object, logger.Object);
    }

    private static Mock<UserManager<AppUser>> BuildUserManager()
    {
        var store = new Mock<IUserStore<AppUser>>();
        var options = Mock.Of<IOptions<IdentityOptions>>();
        var passwordHasher = Mock.Of<IPasswordHasher<AppUser>>();
        var userValidators = Array.Empty<IUserValidator<AppUser>>();
        var passwordValidators = Array.Empty<IPasswordValidator<AppUser>>();
        var normalizer = Mock.Of<ILookupNormalizer>();
        var errorDescriber = new IdentityErrorDescriber();
        var services = Mock.Of<IServiceProvider>();
        var logger = Mock.Of<ILogger<UserManager<AppUser>>>();

        return new Mock<UserManager<AppUser>>(
            store.Object,
            options,
            passwordHasher,
            userValidators,
            passwordValidators,
            normalizer,
            errorDescriber,
            services,
            logger);
    }

    private static Mock<SignInManager<AppUser>> BuildSignInManager(UserManager<AppUser> userManager)
    {
        var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<AppUser>>();
        var options = Mock.Of<IOptions<IdentityOptions>>();
        var logger = Mock.Of<ILogger<SignInManager<AppUser>>>();
        var schemes = Mock.Of<IAuthenticationSchemeProvider>();
        var confirmation = Mock.Of<IUserConfirmation<AppUser>>();

        return new Mock<SignInManager<AppUser>>(
            userManager,
            contextAccessor.Object,
            claimsFactory.Object,
            options,
            logger,
            schemes,
            confirmation);
    }
}
