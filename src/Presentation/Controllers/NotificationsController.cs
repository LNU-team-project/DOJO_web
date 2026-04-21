using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DOJO2.Presentation.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    [HttpGet]
    public IActionResult Settings()
    {
        return View("~/Presentation/Views/Notifications/Settings.cshtml");
    }
}


