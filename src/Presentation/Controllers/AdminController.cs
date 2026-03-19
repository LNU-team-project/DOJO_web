using Microsoft.AspNetCore.Mvc;

namespace DOJO2.Controllers
{
    public class AdminController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
    }
}