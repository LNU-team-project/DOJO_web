using System.Threading.Tasks;
using DOJO2.Controllers;
using DOJO2.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using src.Presentation.ViewModels;
using Xunit;

namespace DOJO_web.Tests
{
    public class AccountControllerPasswordTests
    {
        private readonly Mock<UserManager<AppUser>> _mockUserManager;
        private readonly Mock<IEmailSender> _mockEmailSender;
        private readonly AccountController _controller;

        public AccountControllerPasswordTests()
        {
            // Mocking UserManager
            var userStoreMock = new Mock<IUserStore<AppUser>>();
            _mockUserManager = new Mock<UserManager<AppUser>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            // Mocking IEmailSender
            _mockEmailSender = new Mock<IEmailSender>();

            // Mocking SignInManager (required for controller constructor)
            var contextAccessorMock = new Mock<IHttpContextAccessor>();
            var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<AppUser>>();
            var mockSignInManager = new Mock<SignInManager<AppUser>>(
                _mockUserManager.Object,
                contextAccessorMock.Object,
                claimsFactoryMock.Object,
                null, null, null, null);
            
            // Mocking Logger
            var mockLogger = new Mock<ILogger<AccountController>>();

            // Controller setup
            _controller = new AccountController(
                _mockUserManager.Object,
                mockSignInManager.Object,
                mockLogger.Object,
                _mockEmailSender.Object
            );
        }

        [Fact]
        public async Task ForgotPassword_POST_ValidEmail_SendsEmailAndRedirects()
        {
            // Arrange
            var user = new AppUser { Email = "test@example.com", UserName = "testuser" };
            var model = new ForgotPasswordViewModel { Email = "test@example.com" };
            var token = "test_token";
            
            _mockUserManager.Setup(um => um.FindByEmailAsync(model.Email)).ReturnsAsync(user);
            _mockUserManager.Setup(um => um.GeneratePasswordResetTokenAsync(user)).ReturnsAsync(token);
            _mockEmailSender.Setup(es => es.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            
            // Mock Url.Action
            var urlHelperMock = new Mock<IUrlHelper>();
            urlHelperMock
                .Setup(x => x.Action(It.IsAny<UrlActionContext>()))
                .Returns("callback_url");
            _controller.Url = urlHelperMock.Object;
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await _controller.ForgotPassword(model);

            // Assert
            _mockUserManager.Verify(um => um.FindByEmailAsync(model.Email), Times.Once);
            _mockUserManager.Verify(um => um.GeneratePasswordResetTokenAsync(user), Times.Once);
            _mockEmailSender.Verify(es => es.SendEmailAsync(
                model.Email,
                "Reset Password",
                It.Is<string>(s => s.Contains("callback_url"))), Times.Once);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("ForgotPasswordConfirmation", viewResult.ViewName);
        }

        [Fact]
        public async Task ForgotPassword_POST_InvalidModel_ReturnsView()
        {
            // Arrange
            _controller.ModelState.AddModelError("Email", "Required");
            var model = new ForgotPasswordViewModel();

            // Act
            var result = await _controller.ForgotPassword(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
            _mockEmailSender.Verify(es => es.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ResetPassword_POST_ValidModel_ResetsPasswordAndRedirects()
        {
            // Arrange
            var model = new ResetPasswordViewModel
            {
                Email = "test@example.com",
                Password = "NewPassword123!",
                ConfirmPassword = "NewPassword123!",
                Code = "valid_token"
            };
            var user = new AppUser { Email = model.Email, UserName = "testuser" };

            _mockUserManager.Setup(um => um.FindByEmailAsync(model.Email)).ReturnsAsync(user);
            _mockUserManager.Setup(um => um.ResetPasswordAsync(user, model.Code, model.Password))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.ResetPassword(model);

            // Assert
            _mockUserManager.Verify(um => um.ResetPasswordAsync(user, model.Code, model.Password), Times.Once);
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ResetPasswordConfirmation", redirectToActionResult.ActionName);
        }

        [Fact]
        public async Task ResetPassword_POST_InvalidToken_ReturnsViewWithErrors()
        {
            // Arrange
            var model = new ResetPasswordViewModel
            {
                Email = "test@example.com",
                Password = "NewPassword123!",
                ConfirmPassword = "NewPassword123!",
                Code = "invalid_token"
            };
            var user = new AppUser { Email = model.Email, UserName = "testuser" };
            var error = new IdentityError { Description = "Invalid token." };

            _mockUserManager.Setup(um => um.FindByEmailAsync(model.Email)).ReturnsAsync(user);
            _mockUserManager.Setup(um => um.ResetPasswordAsync(user, model.Code, model.Password))
                .ReturnsAsync(IdentityResult.Failed(error));

            // Act
            var result = await _controller.ResetPassword(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains(_controller.ModelState.Values, v => v.Errors.Any(e => e.ErrorMessage == "Invalid token."));
        }
    }
}
