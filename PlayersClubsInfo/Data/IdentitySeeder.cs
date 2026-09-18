using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlayersClubsInfo.Models;

namespace PlayersClubsInfo.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(
            IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            // Roles
            string[] roles = new[]
            {
                "Root",
                "Manager",
                "User"
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Read default root user from configuration (appsettings.json, user-secrets, env vars, Key Vault, etc.)
            var adminSection = configuration.GetSection("AdminUser");
            var rootUsername = adminSection["Username"] ?? "root";
            var rootEmail = adminSection["Email"] ?? "root@clubsinfo.local";
            var rootPassword = adminSection["Password"];

            if (string.IsNullOrWhiteSpace(rootPassword))
            {
                throw new InvalidOperationException(
                    "AdminUser:Password not configured. Set the password in configuration (appsettings, user-secrets, environment variables, or a secret store).");
            }

            var rootUser = await userManager.FindByNameAsync(rootUsername);

            if (rootUser == null)
            {
                rootUser = new ApplicationUser
                {
                    UserName = rootUsername,
                    Email = rootEmail,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(rootUser, rootPassword);

                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create Root user: {errors}");
                }
            }

            // Ensure rootUser reference is up-to-date (in case it was created above)
            rootUser = await userManager.FindByNameAsync(rootUsername)
                       ?? throw new InvalidOperationException("Root user not found after creation.");

            // Ensure Root role assignment
            if (!await userManager.IsInRoleAsync(rootUser, "Root"))
            {
                var addRoleResult = await userManager.AddToRoleAsync(rootUser, "Root");
                if (!addRoleResult.Succeeded)
                {
                    var errors = string.Join(", ", addRoleResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to assign Root role: {errors}");
                }
            }
        }
    }
}