using Microsoft.EntityFrameworkCore;
using PainForGlory_LoginServer.Models;

namespace PainForGlory_LoginServer.Data
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

        public DbSet<UserAccount> UserAccounts { get; set; }
    }
}
