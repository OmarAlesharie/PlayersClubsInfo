namespace PlayersClubsInfo.DTOs
{
    public class UserResponseDto
    {
        public string Id { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public IList<string> Roles { get; set; } = new List<string>();

        public bool EmailConfirmed { get; set; }

        public bool LockoutEnabled { get; set; }
    }
}
