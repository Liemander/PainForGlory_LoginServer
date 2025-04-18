using System;
using Microsoft.AspNetCore.Identity;

namespace PainForGlory_LoginServer.Models
{
    // Inherit IdentityUser<Guid> so IDs are UUIDs
    public class UserAccount : IdentityUser<Guid>
    {
        // ---- ALIASES to keep old controllers compiling ---------------
        // Your old MVC code calls "Username"; Identity calls it "UserName".
        public string Username
        {
            get => UserName;
            set => UserName = value;
        }
        // --------------------------------------------------------------

        // Extra domain‑specific fields are fine here
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
