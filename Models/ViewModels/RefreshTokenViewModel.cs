namespace PainForGlory_LoginServer.Models.ViewModels
{
    public class RefreshTokenViewModel
    {
        public string Username { get; set; } = default!;
        public string RefreshToken { get; set; } = default!;
    }
}