using DOJO2.Infrastructure.Data;
using DOJO2.Presentation.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace DOJO2.Controllers
{
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasher<string> _passwordHasher;

        public AdminController(AppDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<string>();
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new AdminLoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AdminLoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Login == model.Login);
                if (admin != null)
                {
                    var result = _passwordHasher.VerifyHashedPassword(null, admin.Password, model.Password);
                    if (result == PasswordVerificationResult.Success)
                    {
                        // Успішний вхід
                        return RedirectToAction("LoginSuccess");
                    }
                }
                
                ModelState.AddModelError(string.Empty, "Неправильний логін або пароль");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult LoginSuccess()
        {
            return View();
        }
    }
}