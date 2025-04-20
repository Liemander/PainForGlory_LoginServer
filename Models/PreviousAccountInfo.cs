using PainForGlory_LoginServer.Models;

public class PreviousAccountInfo
{
    public int Id { get; set; }
    public Guid UserAccountId { get; set; }

    public string? OldUsername { get; set; }
    public string? OldEmail { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public UserAccount? UserAccount { get; set; }
}
