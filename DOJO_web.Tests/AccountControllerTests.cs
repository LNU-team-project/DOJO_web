using DOJO2.Controllers;
using DOJO2.Domain.Entities;
using DOJO2.Presentation.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

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

    private static AccountController BuildController(Mock<UserManager<AppUser>>? userManager = null)
    {
        userManager ??= BuildUserManager();
        var signInManager = BuildSignInManager(userManager.Object);
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
