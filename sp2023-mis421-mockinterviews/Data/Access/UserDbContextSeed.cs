using Microsoft.AspNetCore.Identity;
using sp2023_mis421_mockinterviews.Data.Constants;
using sp2023_mis421_mockinterviews.Models.UserDb;
using System;

namespace sp2023_mis421_mockinterviews.Data.Access
{
    public static class UserDbContextSeed
    {
        public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            //Seed Roles
           
            await SeedRoleAsync(roleManager, RolesConstants.AdminRole);
            await SeedRoleAsync(roleManager, RolesConstants.StudentRole);
            await SeedRoleAsync(roleManager, RolesConstants.InterviewerRole);
        }

        private static async Task SeedRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
        {
            // Check if the role already exists
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                // If the role doesn't exist, create it
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        public static async Task SeedSuperAdminAsync(UserManager<ApplicationUser> userManager)
        {
            //Seed Default User
            var defaultUser = new ApplicationUser
            {
                UserName = SuperUser.UserName,
                Email = SuperUser.Email,
                FirstName = SuperUser.FirstName,
                LastName = SuperUser.LastName,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };
            if (userManager.Users.All(u => u.Email != defaultUser.Email))
            {
                var user = await userManager.FindByEmailAsync(defaultUser.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(defaultUser, SuperUser.Password);
                    await userManager.AddToRoleAsync(defaultUser, RolesConstants.AdminRole);
                    await userManager.AddToRoleAsync(defaultUser, RolesConstants.StudentRole);
                    await userManager.AddToRoleAsync(defaultUser, RolesConstants.InterviewerRole);
                }
            }
        }
    }
}
