using IntelligentAttendanceSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IntelligentAttendanceSystem.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Create roles
            string[] roleNames = { "Admin", "Staff", "Student" };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Create admin user
            var adminUser = await userManager.FindByEmailAsync("admin@ams.com");
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin@ams.com",
                    Email = "admin@ams.com",
                    FullName = "System Administrator",
                    Gender = Gender.Male,
                    Birthday = new DateTime(1980, 1, 1),
                    Address = "123 Admin Street",
                    CredentialType = CredentialType.IDCard,
                    CredentialNumber = "ADMIN001",
                    Region = "PK",
                    EmployeeId = "ADMIN001",
                    Department = "Administration",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    EmailConfirmed = true
                };

                var createPowerUser = await userManager.CreateAsync(adminUser, "Admin123!");
                if (createPowerUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}