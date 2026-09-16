namespace PlayersClubsInfo.Models
{
    public class RefreshToken
    {
        public int Id { get; set; }

        // FK to AspNetUsers.Id (ApplicationUser.Id)
        public string UserId { get; set; } = null!;

        // Store only the SHA-256 (or similar) hash of the actual refresh token
        public string TokenHash { get; set; } = null!;

        public DateTime Created { get; set; }
        public DateTime? RevokedAt { get; set; }
        public bool Revoked { get; set; }
        public DateTime Expires { get; set; }

        // When rotating, store the hash of the replacing token for audit/reuse detection
        public string? ReplacedByTokenHash { get; set; }

        // Optional navigation property
        public ApplicationUser? User { get; set; }
    }
}
