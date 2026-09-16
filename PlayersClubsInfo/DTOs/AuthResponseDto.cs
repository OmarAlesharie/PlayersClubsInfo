using System.Data;

namespace PlayersClubsInfo.DTOs
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string Username { get; set; } = string.Empty;
        public IList<string> Roles { get; set; } = new List<string>();
        public string? RefreshToken { get; internal set; }
    }
}
