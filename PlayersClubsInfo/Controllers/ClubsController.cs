using PlayersClubsInfo.Data;
using PlayersClubsInfo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PlayersClubsInfo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClubsController : ControllerBase
    {
        private readonly PlayersClubsInfoContext _context;

        public ClubsController(PlayersClubsInfoContext context)
        {
            _context = context;
        }

        // GET: api/Clubs
        [HttpGet]
        [Authorize(Roles = "Root, Admin, User")]
        public async Task<IActionResult> GetClubs()
        {
            var clubs = await _context.Clubs
            .AsNoTracking()
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
                .FirstOrDefaultAsync(c => c.Id == id);

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
        public async Task<IActionResult> CreateClub(Club club)
        {
            _context.Clubs.Add(club);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetClub),
                new { id = club.Id },
                club);
        }

        // PUT: api/clubs/{id}
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Root,Manager")]
        public async Task<IActionResult> UpdateClub(
            int id,
            Club updatedClub)
        {
            var club = await _context.Clubs.FindAsync(id);

            if (club == null)
            {
                return NotFound(new
                {
                    message = "Club not found."
                });
            }

            club.Name = updatedClub.Name;
            club.City = updatedClub.City;

            await _context.SaveChangesAsync();

            return Ok(club);
        }

        // DELETE: api/clubs/{id}
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Root")]
        public async Task<IActionResult> DeleteClub(int id)
        {
            var club = await _context.Clubs.FindAsync(id);

            if (club == null)
            {
                return NotFound(new
                {
                    message = "Club not found."
                });
            }

            _context.Clubs.Remove(club);

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
