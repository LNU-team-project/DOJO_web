using DOJO2.Application.Common;
using DOJO2.Application.ViewModels;

namespace DOJO2.Application.Interfaces;

public interface INotificationService
{
    Task<Result<IReadOnlyList<DashboardNotificationViewModel>>> GetDashboardNotificationsAsync(int userId, DateTime utcNow);
}

