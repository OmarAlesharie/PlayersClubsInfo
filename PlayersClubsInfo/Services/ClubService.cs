using PlayersClubsInfo.Data;
using PlayersClubsInfo.Models;
using Microsoft.EntityFrameworkCore;

namespace PlayersClubsInfo.Services
{
    public class ClubService
    {
        private readonly PlayersClubsInfoContext _context;
        public ClubService(PlayersClubsInfoContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string? Error)>
        DeleteClubAsync(int clubId)
        {
            var club = await _context.Clubs
                .FirstOrDefaultAsync(c => c.Id == clubId);

            if (club == null)
            {
                return (false, "Club not found.");
            }

            // Release all players belonging to this club.
            var players = await _context.Players
                .Where(p => p.ClubId == clubId)
                .ToListAsync();

            foreach (var player in players)
            {
                player.ClubId = null;
            }

            // Delete the club.
            _context.Clubs.Remove(club);

            await _context.SaveChangesAsync();

            return (true, null);
        }
    }
}
