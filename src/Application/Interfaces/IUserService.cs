using DOJO2.Application.ViewModels;
using DOJO2.Infrastructure.Results;
using Microsoft.AspNetCore.Http;

namespace DOJO2.Application.Interfaces;

public interface IUserService
{
    Task<Result<UserProfileViewModel>> GetUserProfileAsync(int userId);
    Task<Result<UserProfileViewModel>> UpdateUserProfileAsync(int userId, UpdateUserProfileViewModel model);
    Task<Result<bool>> UpdateUserAvatarAsync(int userId, IFormFile avatarFile);
    Task<Result<bool>> DeleteUserAccountAsync(int userId);
}
