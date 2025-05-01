using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using PainForGlory_LoginServer.Data;
using PainForGlory_LoginServer.Models;
using System.Text;
using PainForGlory_LoginServer.Helpers;

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
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "PFG.Identity"; // Optional, to make it easier to debug
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = 401; // Unauthorized
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = 403; // Forbidden
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

// ── YOUR OTHER SERVICES ──────────────────────────────────────────────────────────
builder.Services.AddScoped<TokenService>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin","SuperAdmin")
              .RequireAuthenticatedUser());
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// ── MIDDLEWARE PIPELINE ───────────────────────────────────────────────────────────
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ── ROUTING ───────────────────────────────────────────────────────────────────────
app.MapRazorPages();     // Identity pages like Login/Register
app.MapControllers();    // API controllers

// Public Area - special "root" mapping
app.MapControllerRoute(
    name: "PublicAreaRoot",
    pattern: "{controller=Home}/{action=Index}/{id?}",
    defaults: new { area = "Public" });

// Admin area routes
app.MapAreaControllerRoute(
    name: "AdminArea",
    areaName: "Admin",
    pattern: "admin/{controller=Home}/{action=Index}/{id?}")
    .RequireAuthorization("AdminOnly");

// API area
app.MapAreaControllerRoute(
    name: "ApiArea",
    areaName: "Api",
    pattern: "api/{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var env = services.GetRequiredService<IWebHostEnvironment>();
    await SeedData.InitializeAsync(services);
    if (env.IsDevelopment())
    {
        await SeedData.SeedFakeUsersAsync(services);
    }
}

app.Run();


