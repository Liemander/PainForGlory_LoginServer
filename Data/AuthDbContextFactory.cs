using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PainForGlory_LoginServer.Models;

namespace PainForGlory_LoginServer.Data
{
    public class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
    {
        public AuthDbContext CreateDbContext(string[] args)
        {
            // Load env manually (because WebApplication isn't running)
            DotNetEnv.Env.Load();
            var connectionString = Environment.GetEnvironmentVariable("PFG_LOGIN_DB_CONNECTION");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Missing PFG_LOGIN_DB_CONNECTION env var");
            }

            var optionsBuilder = new DbContextOptionsBuilder<AuthDbContext>();
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            return new AuthDbContext(optionsBuilder.Options);
        }
    }
}

