using Microsoft.AspNetCore.Mvc;
using PainForGlory_LoginServer.Data;
using PainForGlory_LoginServer.Models;
using PainForGlory_LoginServer.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using PainForGlory_LoginServer.Models.ViewModels;

namespace PainForGlory_LoginServer.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthDbContext _context;

        public AccountController(AuthDbContext context)
        {
            _context = context;
        }

        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(UserAccount model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (_context.UserAccounts.Any(u => u.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Username already taken");
                return View(model);
            }

            model.PasswordHash = PasswordHelper.HashPassword(model.PasswordHash);
            model.CreatedAt = DateTime.UtcNow;

            _context.UserAccounts.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }

        public IActionResult Login() => View(new LoginViewModel());

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.UserAccounts.FirstOrDefaultAsync(u => u.Username == model.Username);
            if (user == null || !PasswordHelper.VerifyPassword(model.Password, user.PasswordHash))
            {
                model.ErrorMessage = "Invalid username or password.";
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
