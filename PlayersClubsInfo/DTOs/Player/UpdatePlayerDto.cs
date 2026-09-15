using System.ComponentModel.DataAnnotations;

namespace PlayersClubsInfo.DTOs.Player
{
    public class UpdatePlayerDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(1, 120)]
        public int Age { get; set; }

        [Required]
        [MaxLength(50)]
        public string Position { get; set; } = string.Empty;

        public int? ClubId { get; set; }
    }
}
