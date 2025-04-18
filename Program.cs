using Microsoft.EntityFrameworkCore;
using PainForGlory_LoginServer.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using PainForGlory_LoginServer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.DependencyInjection;
using System.Text;





var builder = WebApplication.CreateBuilder(args);
// Get connection string from environment variable
var connectionString = Environment.GetEnvironmentVariable("PFG_LOGIN_DB_CONNECTION");
DotNetEnv.Env.Load(); // Loads .env file into Environment variables
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

//Old Web server based code
// Add services to the container.
//builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
//
//builder.Services.AddSession();
//builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
//    .AddCookie(options =>
//    {
//        options.LoginPath = "/Account/Login";
//        options.LogoutPath = "/Account/Logout";
//        options.AccessDeniedPath = "/Account/AccessDenied"; // Optional
//    });


// 1️⃣ MVC removed – we only need controllers for now:
builder.Services.AddControllers();          // keeps AccountController working

// 2️⃣ AddIdentityCore keeps your UserAccount table but removes the cookie stack
builder.Services.AddIdentityCore<UserAccount>()
    .AddEntityFrameworkStores<AuthDbContext>();

builder.Services.AddScoped<TokenService>();

// 3️⃣ JWT bearer auth (good for both Unity clients + other servers)
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
        IssuerSigningKey =
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtKey"]))
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseSession();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");




app.Run();
