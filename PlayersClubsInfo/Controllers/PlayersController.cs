using PlayersClubsInfo.Data;
using PlayersClubsInfo.DTOs.Player;
using PlayersClubsInfo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayersClubsInfo.Services;

namespace PlayersClubsInfo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlayersController : ControllerBase
    {
        private readonly PlayersClubsInfoContext _context;
        private readonly PlayerService _playerService;

        public PlayersController(PlayersClubsInfoContext context, PlayerService playerService)
        {
            _context = context;
            _playerService = playerService;
        }

        // GET: api/players
        [HttpGet]
        [Authorize(Roles = "Root,Manager,User")]
        public async Task<IActionResult> GetPlayers()
        {
            var players = await _context.Players
                .AsNoTracking()
                .Select(p => new PlayerResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Age = p.Age,
                    Position = p.Position,
                    ClubId = p.ClubId,
                    ClubName = p.Club != null ? p.Club.Name : null
                })
                .ToListAsync();

            return Ok(players);
        }

        // GET: api/players/{id}
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Root,Manager,User")]
        public async Task<IActionResult> GetPlayer(int id)
        {
            var player = await _context.Players
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new PlayerResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Age = p.Age,
                    Position = p.Position,
                    ClubId = p.ClubId,
                    ClubName = p.Club != null ? p.Club.Name : null
                })
                .FirstOrDefaultAsync();

            if (player == null)
            {
                return NotFound(new
                {
                    message = "Player not found."
                });
            }

            return Ok(player);
        }

        // POST: api/players
        [HttpPost]
        [Authorize(Roles = "Root,Manager")]
        public async Task<IActionResult> CreatePlayer(
            CreatePlayerDto dto)
        {
            // If a club was specified, make sure it exists.
            if (dto.ClubId.HasValue)
            {
                var clubExists = await _context.Clubs
                    .AnyAsync(c => c.Id == dto.ClubId.Value);

                if (!clubExists)
                {
                    return BadRequest(new
                    {
                        message = "The specified club does not exist."
                    });
                }
            }

            var player = new Player
            {
                Name = dto.Name,
                Age = dto.Age,
                Position = dto.Position,
                ClubId = dto.ClubId
            };

            _context.Players.Add(player);

            await _context.SaveChangesAsync();

            var response = await _context.Players
                .AsNoTracking()
                .Where(p => p.Id == player.Id)
                .Select(p => new PlayerResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Age = p.Age,
                    Position = p.Position,
                    ClubId = p.ClubId,
                    ClubName = p.Club != null ? p.Club.Name : null
                })
                .FirstAsync();

            return CreatedAtAction(
                nameof(GetPlayer),
                new { id = player.Id },
                response);
        }

        // PUT: api/players/{id}
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Root,Manager")]
        public async Task<IActionResult> UpdatePlayer(
            int id,
            UpdatePlayerDto dto)
        {
            var player = await _context.Players
                .FindAsync(id);

            if (player == null)
            {
                return NotFound(new
                {
                    message = "Player not found."
                });
            }

            player.Name = dto.Name;
            player.Age = dto.Age;
            player.Position = dto.Position;

            await _context.SaveChangesAsync();

            var response = await _context.Players
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new PlayerResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Age = p.Age,
                    Position = p.Position,
                    ClubId = p.ClubId,
                    ClubName = p.Club != null ? p.Club.Name : null
                })
                .FirstAsync();

            return Ok(response);
        }

        // DELETE: api/players/{id}
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Root")]
        public async Task<IActionResult> DeletePlayer(int id)
        {
            var player = await _context.Players
                .FindAsync(id);

            if (player == null)
            {
                return NotFound(new
                {
                    message = "Player not found."
                });
            }

            _context.Players.Remove(player);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/players/{id}/transfer
        [HttpPost("{id:int}/transfer")]
        [Authorize(Roles = "Root,Manager")]
        public async Task<IActionResult> TransferPlayer(
            int id,
            TransferPlayerDto dto)
        {
            var result = await _playerService.TransferPlayerAsync(
                id,
                dto.ClubId);

            if (!result.Success)
            {
                if (result.Error == "Player not found.")
                {
                    return NotFound(new
                    {
                        message = result.Error
                    });
                }

                return BadRequest(new
                {
                    message = result.Error
                });
            }

            var player = await _context.Players
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new PlayerResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Age = p.Age,
                    Position = p.Position,
                    ClubId = p.ClubId,
                    ClubName = p.Club != null
                        ? p.Club.Name
                        : null
                })
                .FirstAsync();

            return Ok(player);
        }

        // POST: api/players/{id}/release
        [HttpPost("{id:int}/release")]
        [Authorize(Roles = "Root,Manager")]
        public async Task<IActionResult> ReleasePlayer(int id)
        {
            var result = await _playerService.ReleasePlayerAsync(id);

            if (!result.Success)
            {
                if (result.Error == "Player not found.")
                {
                    return NotFound(new
                    {
                        message = result.Error
                    });
                }

                return BadRequest(new
                {
                    message = result.Error
                });
            }

            var player = await _context.Players
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new PlayerResponseDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Age = p.Age,
                    Position = p.Position,
                    ClubId = p.ClubId,
                    ClubName = p.Club != null
                        ? p.Club.Name
                        : null
                })
                .FirstAsync();

            return Ok(player);
        }
    }
}
