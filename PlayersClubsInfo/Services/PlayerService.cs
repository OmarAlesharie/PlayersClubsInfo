using PlayersClubsInfo.Data;
using PlayersClubsInfo.Models;
using Microsoft.EntityFrameworkCore;

namespace PlayersClubsInfo.Services
{
    public class PlayerService
    {
        private readonly PlayersClubsInfoContext _context;

        public PlayerService(PlayersClubsInfoContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string? Error, Player? Player)>
        TransferPlayerAsync(int playerId, int destinationClubId)
        {
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player == null)
            {
                return (false, "Player not found.", null);
            }

            var destinationClub = await _context.Clubs
                .FirstOrDefaultAsync(c => c.Id == destinationClubId);

            if (destinationClub == null)
            {
                return (false, "Destination club not found.", null);
            }

            if (player.ClubId == destinationClubId)
            {
                return (false,
                    "Player already belongs to this club.",
                    null);
            }

            player.ClubId = destinationClubId;

            await _context.SaveChangesAsync();

            return (true, null, player);
        }

        public async Task<(bool Success, string? Error, Player? Player)>
        ReleasePlayerAsync(int playerId)
        {
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player == null)
            {
                return (false, "Player not found.", null);
            }

            if (player.ClubId == null)
            {
                return (false,
                    "Player is already a free agent.",
                    null);
            }

            player.ClubId = null;

            await _context.SaveChangesAsync();

            return (true, null, player);
        }
    }
}
