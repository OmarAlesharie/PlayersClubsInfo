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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Player>()
                .HasOne(p => p.Club)
                .WithMany(c => c.Players)
                .HasForeignKey(p => p.ClubId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
