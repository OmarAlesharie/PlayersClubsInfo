using Microsoft.AspNetCore.Identity;
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

            // Create roles
            string[] roles =
                [
                    "Root",
                    "Manager",
                    "User"
                ];

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Create a default root user
            const string rootUsername = "root";
            const string rootEmail = "root@clubsinfo.local";
            const string rootPassword = "ChangeMe123!";

            var rootUser = await userManager.FindByNameAsync(rootUsername);

            if (rootUser == null)
            {
                rootUser = new ApplicationUser
                {
                    UserName = rootUsername,
                    Email = rootEmail,
                    EmailConfirmed = true
                };

                var result =
                    await userManager.CreateAsync(
                        rootUser,
                        rootPassword);

                if (!result.Succeeded)
                {
                    var errors = string.Join(
                        ", ",
                        result.Errors.Select(e => e.Description));

                    throw new InvalidOperationException(
                        $"Failed to create Root user: {errors}");
                }
            }

            // Make sure Root user has Root role
            if (!await userManager.IsInRoleAsync(rootUser, "Root"))
            {
                var result =
                    await userManager.AddToRoleAsync(
                        rootUser,
                        "Root");

                if (!result.Succeeded)
                {
                    var errors = string.Join(
                        ", ",
                        result.Errors.Select(e => e.Description));

                    throw new InvalidOperationException(
                        $"Failed to assign Root role: {errors}");
                }
            }
        }
    }
}
