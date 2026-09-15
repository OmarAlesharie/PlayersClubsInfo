using PlayersClubsInfo.Data;
using PlayersClubsInfo.DTOs.Club;
using PlayersClubsInfo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlayersClubsInfo.Services;

namespace PlayersClubsInfo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClubsController : ControllerBase
    {
        private readonly PlayersClubsInfoContext _context;
        private readonly ClubService _clubService;

        public ClubsController(PlayersClubsInfoContext context, ClubService clubService)
        {
            _context = context;
            _clubService = clubService;
        }

        // GET: api/Clubs
        // GET: api/clubs
        [HttpGet]
        [Authorize(Roles = "Root,Manager,User")]
        public async Task<IActionResult> GetClubs()
        {
            var clubs = await _context.Clubs
                .AsNoTracking()
                .Select(c => new ClubResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    City = c.City,
                    PlayerCount = c.Players.Count,
                    Players = c.Players
                        .Select(p => new ClubPlayerDto
                        {
                            Id = p.Id,
                            Name = p.Name,
                            Age = p.Age,
                            Position = p.Position
                        })
                        .ToList()
                })
                .ToListAsync();

            return Ok(clubs);
        }

        // GET: api/clubs/{id}
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Root,Manager,User")]
        public async Task<IActionResult> GetClub(int id)
        {
            var club = await _context.Clubs
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new ClubResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    City = c.City,
                    PlayerCount = c.Players.Count,
                    Players = c.Players
                        .Select(p => new ClubPlayerDto
                        {
                            Id = p.Id,
                            Name = p.Name,
                            Age = p.Age,
                            Position = p.Position
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (club == null)
            {
                return NotFound(new
                {
                    message = "Club not found."
                });
            }

            return Ok(club);
        }

        // POST: api/clubs
        [HttpPost]
        [Authorize(Roles = "Root,Manager")]
        public async Task<IActionResult> CreateClub(
            CreateClubDto dto)
        {
            var club = new Club
            {
                Name = dto.Name,
                City = dto.City
            };

            _context.Clubs.Add(club);

            await _context.SaveChangesAsync();

            var response = new ClubResponseDto
            {
                Id = club.Id,
                Name = club.Name,
                City = club.City,
                PlayerCount = 0,
                Players = new List<ClubPlayerDto>()
            };

            return CreatedAtAction(
                nameof(GetClub),
                new { id = club.Id },
                response);
        }

        // PUT: api/clubs/{id}
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Root,Manager")]
        public async Task<IActionResult> UpdateClub(
            int id,
            UpdateClubDto dto)
        {
            var club = await _context.Clubs.FindAsync(id);

            if (club == null)
            {
                return NotFound(new
                {
                    message = "Club not found."
                });
            }

            club.Name = dto.Name;
            club.City = dto.City;

            await _context.SaveChangesAsync();

            var response = await _context.Clubs
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new ClubResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    City = c.City,
                    PlayerCount = c.Players.Count,
                    Players = c.Players
                        .Select(p => new ClubPlayerDto
                        {
                            Id = p.Id,
                            Name = p.Name,
                            Age = p.Age,
                            Position = p.Position
                        })
                        .ToList()
                })
                .FirstAsync();

            return Ok(response);
        }

        // DELETE: api/clubs/{id}
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Root")]
        public async Task<IActionResult> DeleteClub(int id)
        {
            var result = await _clubService.DeleteClubAsync(id);

            if (!result.Success)
            {
                return NotFound(new
                {
                    message = result.Error
                });
            }

            return NoContent();
        }
    }
}