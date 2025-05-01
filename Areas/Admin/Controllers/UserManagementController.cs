using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PainForGlory_LoginServer.Data;
using PainForGlory_LoginServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PainForGlory_LoginServer.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class UserManagementController : Controller
    {
        private readonly UserManager<UserAccount> _userManager;
        private readonly ILogger<UserManagementController> _logger;
        private readonly AuthDbContext _context;

        public UserManagementController(
            UserManager<UserAccount> userManager,
            ILogger<UserManagementController> logger,
            AuthDbContext context)
        {
            _userManager = userManager;
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index(string search, string roleFilter, int page = 1, int pageSize = 10)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var lower = search.ToLower();
                query = query.Where(u =>
                    (u.Email != null && u.Email.ToLower().Contains(lower)) ||
                    (u.UserName != null && u.UserName.ToLower().Contains(lower)));
            }

            if (!string.IsNullOrEmpty(roleFilter))
            {
                var userIdsInRole = await (from ur in _context.UserRoles
                                           join r in _context.Roles on ur.RoleId equals r.Id
                                           where r.Name == roleFilter
                                           select ur.UserId).ToListAsync();

                query = query.Where(u => userIdsInRole.Contains(u.Id));
            }


            int totalUsers = await query.CountAsync();

            if (totalUsers == 0)
            {
                ViewBag.Search = search;
                ViewBag.RoleFilter = roleFilter;
                ViewBag.Page = 1;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalPages = 0;
                ViewBag.TotalUsers = 0;

                return View(new List<UserAccount>());
            }

            int totalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);
            page = Math.Clamp(page, 1, totalPages);

            var pagedUsers = await query
                .OrderBy(u => u.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.RoleFilter = roleFilter;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalUsers = totalUsers;

            return View(pagedUsers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PromoteToAdmin([FromForm] Guid id)
        {
            var currentUserId = Guid.Parse(_userManager.GetUserId(User));
            var user = await _userManager.FindByIdAsync(id.ToString());

            if (user != null && user.Id != currentUserId && !(await _userManager.IsInRoleAsync(user, "SuperAdmin")))
            {
                await _userManager.AddToRoleAsync(user, "Admin");
                _logger.LogInformation("User {User} promoted {Target} to Admin", User.Identity?.Name, user.UserName);
            }

            return RedirectToAction(nameof(Index), new
            {
                area = "Admin",
                search = Request.Form["search"].ToString(),
                roleFilter = Request.Form["roleFilter"].ToString(),
                pageSize = int.TryParse(Request.Form["pageSize"], out var ps) ? ps : 10,
                page = int.TryParse(Request.Form["page"], out var p) ? p : 1
            });

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DemoteAdmin([FromForm] Guid id)
        {
            var currentUserId = Guid.Parse(_userManager.GetUserId(User));
            var user = await _userManager.FindByIdAsync(id.ToString());

            if (user != null && user.Id != currentUserId && !(await _userManager.IsInRoleAsync(user, "SuperAdmin")))
            {
                await _userManager.RemoveFromRoleAsync(user, "Admin");
                _logger.LogInformation("User {User} demoted {Target} from Admin", User.Identity?.Name, user.UserName);
            }

            return RedirectToAction(nameof(Index), new
            {
                area = "Admin",
                search = Request.Form["search"].ToString(),
                roleFilter = Request.Form["roleFilter"].ToString(),
                pageSize = int.TryParse(Request.Form["pageSize"], out var ps) ? ps : 10,
                page = int.TryParse(Request.Form["page"], out var p) ? p : 1
            });

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword([FromForm] Guid id)
        {
            var currentUserId = Guid.Parse(_userManager.GetUserId(User));
            var user = await _userManager.FindByIdAsync(id.ToString());

            if (user != null && user.Id != currentUserId)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var newPassword = "TempPassword123!";
                await _userManager.ResetPasswordAsync(user, token, newPassword);
                _logger.LogWarning("User {User} reset password for {Target}", User.Identity?.Name, user.UserName);
            }

            return RedirectToAction(nameof(Index), new
            {
                area = "Admin",
                search = Request.Form["search"].ToString(),
                roleFilter = Request.Form["roleFilter"].ToString(),
                pageSize = int.TryParse(Request.Form["pageSize"], out var ps) ? ps : 10,
                page = int.TryParse(Request.Form["page"], out var p) ? p : 1
            });

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BanUser([FromForm] Guid id)
        {
            var currentUserId = Guid.Parse(_userManager.GetUserId(User));
            var user = await _userManager.FindByIdAsync(id.ToString());

            if (user != null && user.Id != currentUserId)
            {
                user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
                await _userManager.UpdateAsync(user);
                _logger.LogWarning("User {User} banned {Target}", User.Identity?.Name, user.UserName);
            }

            return RedirectToAction(nameof(Index), new
            {
                area = "Admin",
                search = Request.Form["search"].ToString(),
                roleFilter = Request.Form["roleFilter"].ToString(),
                pageSize = int.TryParse(Request.Form["pageSize"], out var ps) ? ps : 10,
                page = int.TryParse(Request.Form["page"], out var p) ? p : 1
            });

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnbanUser([FromForm] Guid id)
        {
            var currentUserId = Guid.Parse(_userManager.GetUserId(User));
            var user = await _userManager.FindByIdAsync(id.ToString());

            if (user != null && user.Id != currentUserId)
            {
                user.LockoutEnd = null;
                await _userManager.UpdateAsync(user);
                _logger.LogInformation("User {User} unbanned {Target}", User.Identity?.Name, user.UserName);
            }

            return RedirectToAction(nameof(Index), new
            {
                area = "Admin",
                search = Request.Form["search"].ToString(),
                roleFilter = Request.Form["roleFilter"].ToString(),
                pageSize = int.TryParse(Request.Form["pageSize"], out var ps) ? ps : 10,
                page = int.TryParse(Request.Form["page"], out var p) ? p : 1
            });

        }
    }
}
