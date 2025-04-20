using System;
using Microsoft.AspNetCore.Identity;

namespace PainForGlory_LoginServer.Models
{
    // Inherit IdentityUser<Guid> so IDs are UUIDs
    public class UserAccount : IdentityUser<Guid>
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }

        public ICollection<PreviousAccountInfo> PreviousAccountInfos { get; set; } = new List<PreviousAccountInfo>();


    }
}
