using DOJO2.Application.ViewModels;
using DOJO2.Application.Common;

namespace DOJO2.Application.Interfaces;

public interface IUserService
{
    Task<Result<UserProfileViewModel>> GetUserProfileAsync(int userId);
    Task<Result<UserProfileViewModel>> UpdateUserProfileAsync(int userId, UpdateUserProfileViewModel model);
    Task<Result<bool>> UpdateUserAvatarAsync(int userId, FileUploadData avatarFile);
    Task<Result<bool>> DeleteUserAccountAsync(int userId);
}
