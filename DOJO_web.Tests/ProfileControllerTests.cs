using System.Security.Claims;
using DOJO2.Controllers;
using DOJO2.Infrastructure.Results;
using DOJO2.Infrastructure.Services;
using DOJO2.Presentation.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DOJO_web.Tests;

public class ProfileControllerTests
{
    private const int ValidUserId = 123;

    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ILogger<ProfileController>> _loggerMock;

    public ProfileControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _loggerMock = new Mock<ILogger<ProfileController>>();
    }

    [Fact]
    public async Task GetMyProfile_ReturnsUnauthorized_WhenUserIdClaimMissing()
    {
        var controller = CreateControllerWithoutUserIdClaim();

        var result = await controller.GetMyProfile();

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
    }

    [Fact]
    public async Task GetMyProfile_ReturnsNotFound_WhenServiceReturnsFailure()
    {
        var expectedMessage = "Профіль не знайдено";
        var serviceResult = Result<UserProfileViewModel>.FailureResult(expectedMessage);

        _userServiceMock
            .Setup(s => s.GetUserProfileAsync(ValidUserId))
            .ReturnsAsync(serviceResult);

        var controller = CreateControllerWithUserId(ValidUserId);

        var result = await controller.GetMyProfile();

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task GetMyProfile_ReturnsOk_WhenServiceReturnsSuccess()
    {
        var serviceResult = Result<UserProfileViewModel>.SuccessResult(new UserProfileViewModel());

        _userServiceMock
            .Setup(s => s.GetUserProfileAsync(ValidUserId))
            .ReturnsAsync(serviceResult);

        var controller = CreateControllerWithUserId(ValidUserId);

        var result = await controller.GetMyProfile();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_ReturnsBadRequest_WhenModelIsNull()
    {
        var controller = CreateControllerWithUserId(ValidUserId);

        var result = await controller.UpdateProfile(null);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_ReturnsBadRequest_WhenServiceReturnsFailure()
    {
        var updateModel = new UpdateUserProfileViewModel
        {
            UserName = "new_name"
        };

        var serviceResult = Result<UserProfileViewModel>.FailureResult("Не вдалося оновити профіль");

        _userServiceMock
            .Setup(s => s.UpdateUserProfileAsync(ValidUserId, It.IsAny<UpdateUserProfileViewModel>()))
            .ReturnsAsync(serviceResult);

        var controller = CreateControllerWithUserId(ValidUserId);

        var result = await controller.UpdateProfile(updateModel);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_ReturnsOk_WhenServiceReturnsSuccess()
    {
        var updateModel = new UpdateUserProfileViewModel
        {
            UserName = "new_name"
        };

        var serviceResult = Result<UserProfileViewModel>.SuccessResult(new UserProfileViewModel
        {
            UserName = "new_name",
            Email = "andrii@example.com"
        }, "Профіль оновлено");

        _userServiceMock
            .Setup(s => s.UpdateUserProfileAsync(ValidUserId, It.IsAny<UpdateUserProfileViewModel>()))
            .ReturnsAsync(serviceResult);

        var controller = CreateControllerWithUserId(ValidUserId);

        var result = await controller.UpdateProfile(updateModel);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public async Task UploadAvatar_ReturnsBadRequest_WhenAvatarIsNull()
    {
        var controller = CreateControllerWithUserId(ValidUserId);

        var result = await controller.UploadAvatar(null);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task UploadAvatar_ReturnsOk_WhenServiceReturnsSuccess()
    {
        var avatar = CreateTestFormFile();
        var serviceResult = Result<bool>.SuccessResult(true, "Аватар оновлено");

        _userServiceMock
            .Setup(s => s.UpdateUserAvatarAsync(ValidUserId, It.IsAny<IFormFile>()))
            .ReturnsAsync(serviceResult);

        var controller = CreateControllerWithUserId(ValidUserId);

        var result = await controller.UploadAvatar(avatar);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public void Logout_ReturnsUnauthorized_WhenUserIdInvalid()
    {
        var controller = CreateControllerWithUserId(0);

        var result = controller.Logout();

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorizedResult.StatusCode);
    }

    [Fact]
    public void Logout_ReturnsOk_WhenUserIdValid()
    {
        var controller = CreateControllerWithUserId(ValidUserId);

        var result = controller.Logout();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    [Fact]
    public async Task UpdateUserName_ReturnsBadRequest_WhenUserNameEmpty()
    {
        var controller = CreateControllerWithUserId(ValidUserId);

        var result = await controller.UpdateUserName(new UpdateUserProfileViewModel { UserName = "   " });

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task UpdateUserName_ReturnsOk_WhenServiceReturnsSuccess()
    {
        var serviceResult = Result<UserProfileViewModel>.SuccessResult(new UserProfileViewModel
        {
            UserName = "new_name",
            Email = "andrii@example.com"
        }, "Ім'я оновлено");

        _userServiceMock
            .Setup(s => s.UpdateUserProfileAsync(ValidUserId, It.IsAny<UpdateUserProfileViewModel>()))
            .ReturnsAsync(serviceResult);

        var controller = CreateControllerWithUserId(ValidUserId);

        var result = await controller.UpdateUserName(new UpdateUserProfileViewModel { UserName = "new_name" });

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
    }

    private ProfileController CreateControllerWithUserId(int userId)
    {
        var controller = new ProfileController(_userServiceMock.Object, _loggerMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = BuildUser(userId.ToString())
            }
        };

        return controller;
    }

    private ProfileController CreateControllerWithoutUserIdClaim()
    {
        var controller = new ProfileController(_userServiceMock.Object, _loggerMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        return controller;
    }

    private static ClaimsPrincipal BuildUser(string userId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");

        return new ClaimsPrincipal(identity);
    }

    private static IFormFile CreateTestFormFile()
    {
        var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        return new FormFile(stream, 0, stream.Length, "avatar", "avatar.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };
    }
}
