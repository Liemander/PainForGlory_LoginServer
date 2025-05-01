using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PainForGlory_LoginServer.Models;
using System;

namespace PainForGlory_LoginServer.Helpers
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<UserRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<UserAccount>>();
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");

            string fallbackUsername = "superadmin";
            string fallbackEmail = "superadmin@painforglory.net";
            string fallbackPassword = "SuperSecurePassword123!"; // Optional: move this to .env later

            string[] roles = { "Admin", "SuperAdmin" };

            // 1. Ensure roles exist
            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new UserRole { Name = roleName });
                    logger.LogWarning($"Created missing role: {roleName}");
                }
            }

            // 2. Ensure at least one user has the SuperAdmin role
            var superAdmins = await userManager.GetUsersInRoleAsync("SuperAdmin");
            if (superAdmins == null || superAdmins.Count == 0)
            {
                logger.LogError("No users currently have the SuperAdmin role.");

                // Try find fallback user by Username first
                var fallbackUser = await userManager.FindByNameAsync(fallbackUsername);

                // If not found by Username, try by Email
                if (fallbackUser == null)
                {
                    fallbackUser = await userManager.FindByEmailAsync(fallbackEmail);
                }

                if (fallbackUser != null)
                {
                    logger.LogWarning("Fallback user account already exists but has lost the SuperAdmin role. Re-adding role.");

                    var addRoleResult = await userManager.AddToRoleAsync(fallbackUser, "SuperAdmin");
                    if (addRoleResult.Succeeded)
                    {
                        logger.LogWarning("Successfully re-added SuperAdmin role to existing fallback user.");
                    }
                    else
                    {
                        logger.LogCritical("Failed to re-assign SuperAdmin role to existing fallback user!");
                    }
                }
                else
                {
                    // No fallback user at all — safe to create
                    logger.LogError("Fallback user missing completely. Creating a new fallback SuperAdmin user.");

                    fallbackUser = new UserAccount
                    {
                        UserName = fallbackUsername,
                        Email = fallbackEmail,
                        EmailConfirmed = true
                    };

                    var createResult = await userManager.CreateAsync(fallbackUser, fallbackPassword);
                    if (createResult.Succeeded)
                    {
                        await userManager.AddToRoleAsync(fallbackUser, "SuperAdmin");
                        logger.LogError("Fallback SuperAdmin user created and assigned role successfully.");
                    }
                    else
                    {
                        logger.LogCritical("Failed to create fallback SuperAdmin user!");
                    }
                }
            }
            
            

        }

        internal static async Task SeedFakeUsersAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<UserAccount>>();
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SeedFakeUsers");

            var existingFakeUsers = userManager.Users.Any(u => u.Email.EndsWith("@testuser.com"));
            if (existingFakeUsers) return;

            var faker = new Faker();
            var tasks = new List<Task<IdentityResult>>();

            for (int i = 1; i <= 500; i++)
            {
                var first = faker.Name.FirstName();
                var last = faker.Name.LastName();

                var user = new UserAccount
                {
                    UserName = $"{first}{last}{i}".ToLowerInvariant(),
                    Email = $"{first}.{last}.{i}@testuser.com".ToLowerInvariant(),
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, "Test123!");
                if (!result.Succeeded)
                {
                    logger.LogWarning("Failed to create user {User}", user.UserName);
                }
            }

            logger.LogWarning("Seeded 500 fake users with @testuser.com emails.");
        }
    }
}
