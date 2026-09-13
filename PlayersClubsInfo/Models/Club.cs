namespace PlayersClubsInfo.Models
{
    public class Club
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public ICollection<Player> Players { get; set; } = new List<Player>();

    }
}
