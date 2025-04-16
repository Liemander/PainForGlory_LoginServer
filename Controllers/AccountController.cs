using Microsoft.AspNetCore.Mvc;
using PainForGlory_LoginServer.Data;
using PainForGlory_LoginServer.DTOs;
using PainForGlory_LoginServer.Helpers;
using PainForGlory_LoginServer.Models;

namespace PainForGlory_LoginServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly AuthDbContext _context;

        public AccountController(AuthDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            // Check for duplicate usernames
            if (_context.UserAccounts.Any(u => u.Username == request.Username))
                return BadRequest("Username already exists");

            // Hash password
            string hashedPassword = PasswordHelper.HashPassword(request.Password);

            // Create new user
            var user = new UserAccount
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = hashedPassword,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserAccounts.Add(user);
            await _context.SaveChangesAsync();

            return Ok("Account created!");
        }
    }
}
