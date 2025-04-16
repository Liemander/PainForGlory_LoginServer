using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PainForGlory_LoginServer.Data;


namespace PainForGlory_LoginServer
{
    public class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
    {
        public AuthDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AuthDbContext>();
            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION");
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            return new AuthDbContext(optionsBuilder.Options);
        }
    }
}
