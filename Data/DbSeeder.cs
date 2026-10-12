using Healthcare_Industry.Models;
using Microsoft.AspNetCore.Identity;

namespace Healthcare_Industry.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleMgr = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userMgr = services.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in new[] { "Admin", "Customer", "Delivery" })
            if (!await roleMgr.RoleExistsAsync(role))
                await roleMgr.CreateAsync(new IdentityRole(role));

        const string adminEmail = "admin@wellnest.com";
        if (await userMgr.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "System Admin"
            };
            await userMgr.CreateAsync(admin, "Admin@123");
            await userMgr.AddToRoleAsync(admin, "Admin");
        }
    }
}