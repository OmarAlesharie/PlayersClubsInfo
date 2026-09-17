using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PlayersClubsInfo.Data;
using PlayersClubsInfo.DTOs;
using PlayersClubsInfo.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PlayersClubsInfo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController: ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly PlayersClubsInfoContext _db;


        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration, PlayersClubsInfoContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _db = db;
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

            // Generate a refresh token and store it in the database
            var refreshToken = GenerateSecureRefreshToken();
            var refreshTokenRecord = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = HashToken(refreshToken),
                Created = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddDays(7), // adjust lifetime
                Revoked = false
            };

            await _db.RefreshTokens.AddAsync(refreshTokenRecord);
            await _db.SaveChangesAsync();


            var roles = await _userManager.GetRolesAsync(user);
            var token = GenerateJwtToken(user, roles);

            return Ok(new AuthResponseDto
            {
                Token = token.Token,
                ExpiresAt = token.ExpiresAt,
                Username = user.UserName!,
                Roles = roles,
                RefreshToken = refreshToken
            });
        }

        // POST: api/auth/logout
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] RevokeRequestDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            // 1) Revoke refresh token if provided
            if (!string.IsNullOrWhiteSpace(dto?.RefreshToken))
            {
                var hash = HashToken(dto.RefreshToken);
                var rtoken = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash && t.UserId == userId);
                if (rtoken != null && !rtoken.Revoked)
                {
                    rtoken.Revoked = true;
                    rtoken.RevokedAt = DateTime.UtcNow;
                }
            }

            // 2) Revoke current access token immediately by storing its JTI
            var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti) ?? User.FindFirstValue("jti");
            if (!string.IsNullOrEmpty(jti))
            {
                // get exp claim to set expiry for cleanup
                DateTime expiresAt;
                var expClaim = User.FindFirstValue(JwtRegisteredClaimNames.Exp);
                if (!string.IsNullOrEmpty(expClaim) && long.TryParse(expClaim, out var seconds))
                    expiresAt = DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
                else
                    expiresAt = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:ExpirationMinutes"));

                _db.RevokedAccessTokens.Add(new RevokedAccessToken
                {
                    Jti = jti,
                    ExpiresAt = expiresAt
                });
            }

            await _db.SaveChangesAsync();

            return Ok(new { message = "Logout successful. Refresh token revoked and access token invalidated." });
        }

        [HttpPost("logout-all")]
        [Authorize]
        public async Task<IActionResult> LogoutAll()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var tokens = _db.RefreshTokens.Where(t => t.UserId == userId && !t.Revoked);
            await tokens.ForEachAsync(t => {
                t.Revoked = true;
                t.RevokedAt = DateTime.UtcNow;
            });

            await _db.SaveChangesAsync();

            return Ok(new { message = "All refresh tokens revoked." });
        }

        private (string Token, DateTime ExpiresAt) GenerateJwtToken( ApplicationUser user, IList<string> roles)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured.");
            var issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured.");
            var audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured.");
            var expirationMinutes = _configuration.GetValue<int>("Jwt:ExpirationMinutes");
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

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key,SecurityAlgorithms.HmacSha256);

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
        public async Task<IActionResult> ChangePassword(ChangeOwnPasswordDto dto)
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

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.RefreshToken))
                return BadRequest(new { message = "Refresh token is required." });

            var incomingHash = HashToken(dto.RefreshToken);
            var existing = await _db.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == incomingHash);

            if (existing == null || existing.Revoked || existing.Expires <= DateTime.UtcNow)
                return Unauthorized(new { message = "Invalid or expired refresh token." });

            // rotate: revoke existing and create new
            existing.Revoked = true;
            existing.RevokedAt = DateTime.UtcNow;

            var newRefreshToken = GenerateSecureRefreshToken();
            var newRecord = new RefreshToken
            {
                UserId = existing.UserId,
                TokenHash = HashToken(newRefreshToken),
                Created = DateTime.UtcNow,
                Expires = DateTime.UtcNow.AddDays(7),
                Revoked = false
            };
            existing.ReplacedByTokenHash = newRecord.TokenHash;

            _db.RefreshTokens.Add(newRecord);
            await _db.SaveChangesAsync();

            var user = await _userManager.FindByIdAsync(existing.UserId);
            var roles = await _userManager.GetRolesAsync(user!);
            var newJwt = GenerateJwtToken(user!, roles);

            return Ok(new AuthResponseDto
            {
                Token = newJwt.Token,
                ExpiresAt = newJwt.ExpiresAt,
                Username = user!.UserName!,
                Roles = roles,
                RefreshToken = newRefreshToken
            });
        }

        private static string HashToken(string token)
        {
            using var sha = SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(token);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private static string GenerateSecureRefreshToken()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}
