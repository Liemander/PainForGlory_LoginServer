using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PainForGlory_LoginServer.Helpers;
using PainForGlory_LoginServer.Models;

namespace PainForGlory_LoginServer.Areas.Public.Controllers
{
    [Area("Public")]
    [Authorize] // ✅ Require authentication but no role
    public class EmergencyRepairController : Controller
    {
        [HttpPost]
        public async Task<IActionResult> CheckAndRepairSuperAdmin()
        {
            var username = User.Identity?.Name ?? "Unknown";
            Console.WriteLine($"SuperAdmin repair requested by user: {username} at {DateTime.UtcNow} UTC");

            await SeedData.InitializeAsync(HttpContext.RequestServices);

            TempData["Message"] = "SuperAdmin check complete.";
            return RedirectToAction("Index", "Home", new { area = "Public" });
        }
    }
}
