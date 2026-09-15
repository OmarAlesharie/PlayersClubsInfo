using System.ComponentModel.DataAnnotations;

namespace PlayersClubsInfo.DTOs.Club
{
    public class UpdateClubDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;
    }
}
