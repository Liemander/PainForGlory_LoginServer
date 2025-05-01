using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using PainForGlory_LoginServer.Models;
using PainForGlory_LoginServer.Models.ViewModels;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using PainForGlory_LoginServer.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;


namespace PainForGlory_LoginServer.Areas.API.Controllers
{
    [Area("Api")]
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<UserAccount> _userManager;
        private readonly IConfiguration _config;
        private readonly AuthDbContext _context;

        // Constructor to inject dependencies
        public AccountController(
            UserManager<UserAccount> userManager,
            IConfiguration config,
            AuthDbContext context)
        {
            _userManager = userManager;
            _config = config;
            _context = context;
        }

        // Endpoint for user login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            // Find user by username
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                return Unauthorized(); // Return 401 if user not found or password is incorrect

            // Generate a new refresh token and set its expiry
            var refreshToken = GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            // Update user with the new refresh token
            await _userManager.UpdateAsync(user);

            // Generate a JWT access token
            var accessToken = GenerateAccessToken(user);

            // Return both access and refresh tokens
            return Ok(new
            {
                accessToken,
                refreshToken
            });
        }

        // Endpoint to refresh JWT access token using a valid refresh token
        [HttpPost("refresh-token")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenViewModel model)
        {
            // Find user by username
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null || user.RefreshToken != model.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return Unauthorized("Invalid or expired refresh token");

            // Generate new access and refresh tokens
            var newAccessToken = GenerateAccessToken(user);
            var newRefreshToken = GenerateRefreshToken();

            // Update user with the new refresh token
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userManager.UpdateAsync(user);

            // Return the new tokens
            return Ok(new
            {
                accessToken = newAccessToken,
                refreshToken = newRefreshToken
            });
        }

        // Helper method to generate a JWT access token
        private string GenerateAccessToken(UserAccount user)
        {
            // Define claims for the token
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName)
            };

            // Create signing credentials using the secret key
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Create the token with an expiration time
            var token = new JwtSecurityToken(
                expires: DateTime.UtcNow.AddMinutes(30),
                claims: claims,
                signingCredentials: creds
            );

            // Return the serialized token
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Helper method to generate a secure random refresh token
        private string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }

        // Endpoint for user registration
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            // Validate the model
            if (!ModelState.IsValid)
                return BadRequest("Invalid registration data.");

            // Create a new user account
            var user = new UserAccount
            {
                UserName = model.Username,
                Email = model.Email,
                CreatedAt = DateTime.UtcNow
            };

            // Attempt to create the user with the provided password
            var result = await _userManager.CreateAsync(user, model.Password);

            // Return errors if registration fails
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok("Registration successful.");
        }

        [HttpPost("update")]
        [Authorize]
        public async Task<IActionResult> UpdateAccount([FromBody] UpdateAccountViewModel model)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized("Missing or invalid token.");

            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
                return NotFound("User not found");

            if (string.IsNullOrWhiteSpace(model.CurrentPassword) || !await _userManager.CheckPasswordAsync(user, model.CurrentPassword))
                return Unauthorized("Invalid current password");

            bool usernameChanged = !string.IsNullOrWhiteSpace(model.NewUsername) && model.NewUsername != user.UserName;
            bool emailChanged = !string.IsNullOrWhiteSpace(model.NewEmail) && model.NewEmail != user.Email;

            if (!usernameChanged && !emailChanged && string.IsNullOrWhiteSpace(model.NewPassword))
                return BadRequest("No changes detected");

            var previousInfo = new PreviousAccountInfo
            {
                UserAccountId = user.Id,
                OldUsername = usernameChanged ? user.UserName : null,
                OldEmail = emailChanged ? user.Email : null,
                ChangedAt = DateTime.UtcNow
            };

            if (usernameChanged) user.UserName = model.NewUsername!;
            if (emailChanged) user.Email = model.NewEmail!;

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
                if (!passwordResult.Succeeded)
                    return BadRequest(passwordResult.Errors);
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            _context.PreviousAccountInfos.Add(previousInfo);
            await _context.SaveChangesAsync();

            var newAccessToken = GenerateAccessToken(user);
            return Ok(new { accessToken = newAccessToken });
        }

        [Authorize]
        [HttpGet("info")]
        public async Task<IActionResult> GetAccountInfo()
        {

            var username = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(username)) return Unauthorized();

            var user = await _userManager.FindByNameAsync(username);
            if (user == null) return NotFound("User not found");

            return Ok(new { user.Email });


        }

        [Authorize]
        [HttpGet("history")]
        public async Task<IActionResult> GetAccountHistory()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(username)) return Unauthorized();

            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
                return NotFound("User not found");

            var history = await _context.PreviousAccountInfos
                .Where(p => p.UserAccountId == user.Id)
                .OrderByDescending(p => p.ChangedAt)
                .Select(h => new PreviousAccountInfo
                {
                    OldUsername = h.OldUsername,
                    OldEmail = h.OldEmail,
                    ChangedAt = h.ChangedAt,
                    Id = h.Id,
                    UserAccountId = h.UserAccountId
                }).ToListAsync();
            

            return Ok(history);
        }


    }
}
