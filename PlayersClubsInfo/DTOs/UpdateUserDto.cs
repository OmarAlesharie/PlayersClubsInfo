using System.ComponentModel.DataAnnotations;

namespace PlayersClubsInfo.DTOs
{
    public class UpdateUserDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "User";

        public bool LockoutEnabled { get; set; }
    }
}
