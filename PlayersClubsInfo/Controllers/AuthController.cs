using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using PlayersClubsInfo.DTOs;
using PlayersClubsInfo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;

namespace PlayersClubsInfo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController: ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;


        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var existingUser = await _userManager.FindByNameAsync(dto.Username);

            if (existingUser != null)
            {
                return Conflict(new
                {
                    message = "Username is already registered"
                });
            }

            var existingEmail = await _userManager.FindByEmailAsync(dto.Email);

            if (existingEmail != null)
            {
                return Conflict(new
                {
                    message = "Email is already registered"
                });
            }

            var user = new ApplicationUser
            {
                UserName = dto.Username,
                Email = dto.Email,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            // Every publicly registered user will be assigned the "User" role by default
            var roleResult = await _userManager.AddToRoleAsync(user, "User");

            if (!roleResult.Succeeded)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message = "User was created but could not be assigned a role."
                    });
            }

            return StatusCode(StatusCodes.Status201Created, new
            {
                message = "User registered successfully"
            });

        }

        //POST: api/auth/login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _userManager.FindByNameAsync(dto.Username);
            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid username or password."
                });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                return Unauthorized(new
                {
                    message = "Invalid username or password."
                });
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = GenerateJwtToken(user, roles);

            return Ok(new AuthResponseDto
            {
                Token = token.Token,
                ExpiresAt = token.ExpiresAt,
                Username = user.UserName!,
                Roles = roles
            });
        }

        // POST: api/auth/logout
        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            /*
             * JWT authentication is stateless.
             *
             * There is no server-side session to destroy.
             * The client should delete its JWT after receiving
             * this response.
             */

            return Ok(new
            {
                message = "Logout successful. Please remove the JWT from the client."
            });
        }

        private (string Token, DateTime ExpiresAt) GenerateJwtToken(
        ApplicationUser user,
        IList<string> roles)
        {
            var jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "JWT Key is not configured.");

            var issuer = _configuration["Jwt:Issuer"]
                ?? throw new InvalidOperationException(
                    "JWT Issuer is not configured.");

            var audience = _configuration["Jwt:Audience"]
                ?? throw new InvalidOperationException(
                    "JWT Audience is not configured.");

            var expirationMinutes =
                _configuration.GetValue<int>("Jwt:ExpirationMinutes");

            var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName!),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

            return (
                new JwtSecurityTokenHandler().WriteToken(token),
                expiresAt);
        }

        // PUT: api/auth/change-password
        [HttpPut("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(
            ChangeOwnPasswordDto dto)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new
                {
                    message = "User identity could not be determined."
                });
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound(new
                {
                    message = "User not found."
                });
            }

            var result = await _userManager.ChangePasswordAsync(
                user,
                dto.CurrentPassword,
                dto.NewPassword);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            return Ok(new
            {
                message = "Password changed successfully."
            });
        }
    }
}
