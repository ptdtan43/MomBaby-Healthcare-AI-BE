using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MomOi.API.Models.Identity;
using System;
using System.Threading.Tasks;

namespace MomOi.API.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();

            // 1. Seed Roles
            string[] roles = { AppRoles.Admin, AppRoles.Staff, AppRoles.Expert, AppRoles.Mom };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Seed Admin User
            var adminEmail = "admin@momoi.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                var admin = new AppUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Admin",
                    Tier = SubscriptionTier.SuperMomVip,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(admin, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, AppRoles.Admin);
                }
            }
            else if (!await userManager.IsInRoleAsync(adminUser, AppRoles.Admin))
            {
                await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
            }

            // 3. Seed Staff User
            var staffEmail = "staff@momoi.com";
            var staffUser = await userManager.FindByEmailAsync(staffEmail);
            if (staffUser == null)
            {
                var staff = new AppUser
                {
                    UserName = staffEmail,
                    Email = staffEmail,
                    FullName = "Care Staff",
                    Tier = SubscriptionTier.SuperMomVip,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(staff, "Staff@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(staff, AppRoles.Staff);
                }
            }
            else if (!await userManager.IsInRoleAsync(staffUser, AppRoles.Staff))
            {
                await userManager.AddToRoleAsync(staffUser, AppRoles.Staff);
            }

            // 4. Seed Expert User
            var expertEmail = "expert@momoi.com";
            var expertUser = await userManager.FindByEmailAsync(expertEmail);
            if (expertUser == null)
            {
                var expert = new AppUser
                {
                    UserName = expertEmail,
                    Email = expertEmail,
                    FullName = "Medical Expert",
                    Tier = SubscriptionTier.SuperMomVip,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(expert, "Expert@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(expert, AppRoles.Expert);
                }
            }
            else if (!await userManager.IsInRoleAsync(expertUser, AppRoles.Expert))
            {
                await userManager.AddToRoleAsync(expertUser, AppRoles.Expert);
            }
        }
    }
}
