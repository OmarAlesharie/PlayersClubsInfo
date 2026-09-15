using System.ComponentModel.DataAnnotations;

namespace PlayersClubsInfo.DTOs
{
    public class ChangePasswordDto
    {
        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
