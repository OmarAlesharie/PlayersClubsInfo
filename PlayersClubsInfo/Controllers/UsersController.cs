using PlayersClubsInfo.DTOs;
using PlayersClubsInfo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace PlayersClubsInfo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Root")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: api/users
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = _userManager.Users.ToList();

            var result = new List<UserResponseDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                result.Add(new UserResponseDto
                {
                    Id = user.Id,
                    Username = user.UserName!,
                    Email = user.Email!,
                    Roles = roles,
                    EmailConfirmed = user.EmailConfirmed,
                    LockoutEnabled = user.LockoutEnabled
                });
            }

            return Ok(result);
        }

        // GET: api/users/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound(new
                {
                    message = "User not found."
                });
            }

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new UserResponseDto
            {
                Id = user.Id,
                Username = user.UserName!,
                Email = user.Email!,
                Roles = roles,
                EmailConfirmed = user.EmailConfirmed,
                LockoutEnabled = user.LockoutEnabled
            });
        }

        // POST: api/users
        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateUserDto dto)
        {
            if (!await _roleManager.RoleExistsAsync(dto.Role))
            {
                return BadRequest(new
                {
                    message = $"Role '{dto.Role}' does not exist."
                });
            }

            if (await _userManager.FindByNameAsync(dto.Username) != null)
            {
                return Conflict(new
                {
                    message = "Username is already registered."
                });
            }

            if (await _userManager.FindByEmailAsync(dto.Email) != null)
            {
                return Conflict(new
                {
                    message = "Email is already registered."
                });
            }

            var user = new ApplicationUser
            {
                UserName = dto.Username,
                Email = dto.Email,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(
                user,
                dto.Password);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            var roleResult = await _userManager.AddToRoleAsync(
                user,
                dto.Role);

            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message = "User was created but could not be assigned the role."
                    });
            }

            return CreatedAtAction(
                nameof(GetUser),
                new { id = user.Id },
                new UserResponseDto
                {
                    Id = user.Id,
                    Username = user.UserName!,
                    Email = user.Email!,
                    Roles = new List<string> { dto.Role },
                    EmailConfirmed = user.EmailConfirmed,
                    LockoutEnabled = user.LockoutEnabled
                });
        }

        // PUT: api/users/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(
            string id,
            UpdateUserDto dto)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound(new
                {
                    message = "User not found."
                });
            }

            if (!await _roleManager.RoleExistsAsync(dto.Role))
            {
                return BadRequest(new
                {
                    message = $"Role '{dto.Role}' does not exist."
                });
            }

            var emailOwner = await _userManager.FindByEmailAsync(dto.Email);

            if (emailOwner != null && emailOwner.Id != user.Id)
            {
                return Conflict(new
                {
                    message = "Email is already in use."
                });
            }

            user.Email = dto.Email;
            user.LockoutEnabled = dto.LockoutEnabled;

            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                return BadRequest(new
                {
                    errors = updateResult.Errors.Select(e => e.Description)
                });
            }

            var currentRoles = await _userManager.GetRolesAsync(user);

            if (currentRoles.Count > 0)
            {
                var removeResult =
                    await _userManager.RemoveFromRolesAsync(
                        user,
                        currentRoles);

                if (!removeResult.Succeeded)
                {
                    return StatusCode(
                        StatusCodes.Status500InternalServerError,
                        new
                        {
                            message = "Failed to update user's existing role."
                        });
                }
            }

            var roleResult =
                await _userManager.AddToRoleAsync(user, dto.Role);

            if (!roleResult.Succeeded)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message = "Failed to assign the new role."
                    });
            }

            return Ok(new UserResponseDto
            {
                Id = user.Id,
                Username = user.UserName!,
                Email = user.Email!,
                Roles = new List<string> { dto.Role },
                EmailConfirmed = user.EmailConfirmed,
                LockoutEnabled = user.LockoutEnabled
            });
        }

        // DELETE: api/users/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound(new
                {
                    message = "User not found."
                });
            }

            // Prevent Root from deleting their own account.
            if (User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier)?.Value == id)
            {
                return BadRequest(new
                {
                    message = "You cannot delete your own account."
                });
            }

            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            return NoContent();
        }

        // PUT: api/users/{id}/password
        [HttpPut("{id}/password")]
        public async Task<IActionResult> ChangePassword(
            string id,
            ChangePasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound(new
                {
                    message = "User not found."
                });
            }

            // Generate a password-reset token.
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            var result = await _userManager.ResetPasswordAsync(
                user,
                resetToken,
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
