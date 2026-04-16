using DOJO2.Application.ViewModels;
using DOJO2.Application.Common;

namespace DOJO2.Application.Interfaces;

public interface IAdminService
{
    Task<Result<bool>> AuthenticateAdminAsync(string login, string password);
    Task<Result<List<AdminUserListItemViewModel>>> GetUsersAsync(string? search);
    Task<Result<bool>> BlockUserAsync(int userId);
    Task<Result<bool>> UnblockUserAsync(int userId);
    Task<Result<bool>> DeleteUserAsync(int userId);
}
