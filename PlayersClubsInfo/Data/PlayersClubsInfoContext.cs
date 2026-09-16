using PlayersClubsInfo.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace PlayersClubsInfo.Data
{
    public class PlayersClubsInfoContext : IdentityDbContext<ApplicationUser>
    {
        public PlayersClubsInfoContext(DbContextOptions<PlayersClubsInfoContext> options) : base(options)
        {
        }

        public DbSet<Club> Clubs => Set<Club>();
        public DbSet<Player> Players => Set<Player>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Player>()
                .HasOne(p => p.Club)
                .WithMany(c => c.Players)
                .HasForeignKey(p => p.ClubId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.UserId)
                    .IsRequired();

                // TokenHash is base64-encoded SHA-256 (≈44 chars); allow a bit more room
                entity.Property(e => e.TokenHash)
                    .IsRequired()
                    .HasMaxLength(128);

                entity.Property(e => e.Created)
                    .IsRequired();

                entity.Property(e => e.Expires)
                    .IsRequired();

                entity.Property(e => e.Revoked)
                    .HasDefaultValue(false)
                    .IsRequired();

                entity.Property(e => e.RevokedAt)
                    .IsRequired(false);

                entity.Property(e => e.ReplacedByTokenHash)
                    .HasMaxLength(128)
                    .IsRequired(false);

                // Unique index to speed lookup by token hash (one active token per hash)
                entity.HasIndex(e => e.TokenHash).IsUnique();

                // FK to AspNetUsers (ApplicationUser). Cascade delete tokens when user removed.
                entity.HasOne(e => e.User)
                    .WithMany() // add navigation on ApplicationUser if desired
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
