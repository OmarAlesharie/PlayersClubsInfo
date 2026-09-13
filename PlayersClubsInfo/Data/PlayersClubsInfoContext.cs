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
    }
}
