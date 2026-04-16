using DOJO2.Application.ViewModels;
using DOJO2.Domain.Entities;
using DOJO2.Application.Common;

namespace DOJO2.Application.Interfaces;

public interface IAuthService
{
    Task<Result<bool>> LoginAsync(string email, string password, bool rememberMe);
    Task<Result<bool>> RegisterAsync(string userName, string email, string password);
    Task<Result<bool>> LogoutAsync();
    Task<Result<bool>> ForgotPasswordAsync(string email, string callbackUrl);
    Task<Result<bool>> ResetPasswordAsync(string email, string code, string newPassword);
    Task<Result<AppUser>> GetUserAsync(string userId);
    Task<Result<bool>> SendEmailConfirmationAsync(string email, string callbackUrl);
    Task<Result<bool>> ConfirmEmailAsync(int userId, string code);
    Task<Result<bool>> SendTestEmailAsync(string email);
}
