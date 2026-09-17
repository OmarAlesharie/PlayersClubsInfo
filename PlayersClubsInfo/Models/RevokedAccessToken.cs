namespace PlayersClubsInfo.Models
{
    public class RevokedAccessToken
    {
        public int Id { get; set; }
        // Token JTI claim
        public string Jti { get; set; } = null!;
        // When the token expires (UTC)
        public DateTime ExpiresAt { get; set; }
    }
}
