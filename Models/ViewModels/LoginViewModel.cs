// Models/ViewModels/LoginViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace PainForGlory_LoginServer.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
        public bool RememberMe { get; internal set; } = false;
    }
}
