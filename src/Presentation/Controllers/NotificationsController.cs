using DOJO2.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DOJO2.Controllers;

[Authorize]
public class NotificationsController : BaseApiController
{
	private readonly INotificationService _notificationService;

	public NotificationsController(INotificationService notificationService, ILogger<NotificationsController> logger)
		: base(logger)
	{
		_notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
	}

	[HttpGet("dashboard")]
	public async Task<IActionResult> GetDashboardNotifications()
	{
		var authError = ValidateUserAuthorization();
		if (authError != null)
		{
			return authError;
		}

		var userId = GetCurrentUserId() ?? 0;
		var result = await _notificationService.GetDashboardNotificationsAsync(userId, DateTime.UtcNow);

		return ToActionResultWithNotFound(result);
	}
}
