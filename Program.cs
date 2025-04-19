using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using PainForGlory_LoginServer.Data;
using PainForGlory_LoginServer.Models;
using System.Text;

DotNetEnv.Env.Load(); // Load .env into Environment variables

var builder = WebApplication.CreateBuilder(args);

// ✅ Get connection string
var connectionString = Environment.GetEnvironmentVariable("PFG_LOGIN_DB_CONNECTION");
if (string.IsNullOrEmpty(connectionString))
    throw new InvalidOperationException("Database connection string is missing.");

// ✅ Configure EF + Identity
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddIdentityCore<UserAccount>(options =>
{
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AuthDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddScoped<TokenService>();

// ✅ Configure JWT authentication
var jwtKey = builder.Configuration["JwtKey"] ?? Environment.GetEnvironmentVariable("JwtKey");
if (string.IsNullOrEmpty(jwtKey))
    throw new InvalidOperationException("JwtKey configuration is missing or empty.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opt =>
{
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

// ✅ Use only controllers (API)
builder.Services.AddControllers();

var app = builder.Build();

// ✅ Clean middleware
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ✅ Map API routes
app.MapControllers();

// Optional: add health check endpoint
app.MapGet("/", () => Results.Json(new { status = "Login server is running" }));

app.Run();
