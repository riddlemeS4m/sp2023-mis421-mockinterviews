using Microsoft.AspNetCore.Identity;
using sp2023_mis421_mockinterviews.Data.Constants;
using sp2023_mis421_mockinterviews.Models.UserDb;
using System;

namespace sp2023_mis421_mockinterviews.Data.Access
{
    public static class UserDbContextSeed
    {
        public static async Task SeedRolesAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            //Seed Roles
            await roleManager.CreateAsync(new IdentityRole(RolesConstants.AdminRole));
            await roleManager.CreateAsync(new IdentityRole(RolesConstants.StudentRole));
            await roleManager.CreateAsync(new IdentityRole(RolesConstants.InterviewerRole));
        }

        public static async Task SeedSuperAdminAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
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
            if (userManager.Users.All(u => u.Id != defaultUser.Id))
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
