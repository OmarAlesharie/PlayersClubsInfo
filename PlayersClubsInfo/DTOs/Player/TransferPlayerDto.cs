using System.ComponentModel.DataAnnotations;

namespace PlayersClubsInfo.DTOs.Player
{
    public class TransferPlayerDto
    {
        [Required]
        public int ClubId { get; set; }
    }
}
