using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using PainForGlory_LoginServer.Data;
using PainForGlory_LoginServer.Models;
using System.Text;

DotNetEnv.Env.Load(); // Load .env into Environment variables

var builder = WebApplication.CreateBuilder(args);

// ── DATABASE & DB CONTEXT ────────────────────────────────────────────────────────
var connectionString = Environment.GetEnvironmentVariable("PFG_LOGIN_DB_CONNECTION")
    ?? throw new InvalidOperationException("Database connection string is missing.");

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// ── IDENTITY (UserAccount + UserRole) ─────────────────────────────────────────────
builder.Services
    .AddIdentity<UserAccount, UserRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

// ── AUTHENTICATION (ONLY ADD JWT AFTER IDENTITY) ──────────────────────────────────
var jwtKey = builder.Configuration["JwtKey"]
             ?? Environment.GetEnvironmentVariable("JwtKey")
             ?? throw new InvalidOperationException("JwtKey configuration is missing.");

builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, jwtOpts =>
    {
        jwtOpts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// ── YOUR OTHER SERVICES ──────────────────────────────────────────────────────────
builder.Services.AddScoped<TokenService>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin")
              .RequireAuthenticatedUser());
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// ── MIDDLEWARE PIPELINE ───────────────────────────────────────────────────────────
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ── ROUTING ───────────────────────────────────────────────────────────────────────
app.MapRazorPages();     // Identity pages like Login/Register
app.MapControllers();    // API controllers

// Public routes (default Home, etc.)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Admin area routes
app.MapAreaControllerRoute(
    name: "AdminArea",
    areaName: "Admin",
    pattern: "admin/{controller=Home}/{action=Index}/{id?}")
   .RequireAuthorization("AdminOnly");

// Optional: Health check endpoint
app.MapGet("/", () => Results.Json(new { status = "Login server is running" }));

app.Run();
