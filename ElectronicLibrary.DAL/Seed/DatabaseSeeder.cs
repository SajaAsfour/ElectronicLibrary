using ElectronicLibrary.DAL.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ElectronicLibrary.DAL.Seed;

public static class DatabaseSeeder
{
    private static readonly string[] Roles =
    [
        "Admin",
        "Customer",
        "Seller"
    ];

    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        using var scope = services.CreateScope();

        var roleManager =
            scope.ServiceProvider
                .GetRequiredService<
                    RoleManager<IdentityRole>>();

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<
                    UserManager<ApplicationUser>>();

        foreach (var roleName in Roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(
                    new IdentityRole(roleName));
            }
        }

        var adminEmail =
            configuration["SeedAdmin:Email"];

        var adminPassword =
            configuration["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(adminEmail) ||
            string.IsNullOrWhiteSpace(adminPassword))
        {
            return;
        }

        var admin =
            await userManager.FindByEmailAsync(
                adminEmail);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                FullName = "System Administrator",
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var createResult =
                await userManager.CreateAsync(
                    admin,
                    adminPassword);

            if (!createResult.Succeeded)
            {
                var errors =
                    string.Join(
                        ", ",
                        createResult.Errors.Select(
                            x => x.Description));

                throw new InvalidOperationException(
                    errors);
            }
        }

        if (!await userManager.IsInRoleAsync(
                admin,
                "Admin"))
        {
            await userManager.AddToRoleAsync(
                admin,
                "Admin");
        }
    }
}