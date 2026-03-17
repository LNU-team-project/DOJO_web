using Microsoft.AspNetCore.Mvc;

namespace DOJO2.Controllers;

public class AccountController : Controller
{
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }
}
