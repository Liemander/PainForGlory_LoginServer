using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PainForGlory_LoginServer.Models;

namespace PainForGlory_LoginServer.Data
{
    public class AuthDbContext
        : IdentityDbContext<UserAccount, IdentityRole<Guid>, Guid>
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

        // ---- ALIAS for legacy code ----------------------------------
        public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
        // --------------------------------------------------------------

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // (rename tables here if you like)
        }
    }
}
