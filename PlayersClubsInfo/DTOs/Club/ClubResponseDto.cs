namespace PlayersClubsInfo.DTOs.Club
{
    public class ClubResponseDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public int PlayerCount { get; set; }

        public List<ClubPlayerDto> Players { get; set; } = new();
    }

    public class ClubPlayerDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int Age { get; set; }

        public string Position { get; set; } = string.Empty;
    }
}


