using ElectronicLibrary.DAL.Constants;
using ElectronicLibrary.DAL.Data;
using ElectronicLibrary.DAL.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ElectronicLibrary.DAL.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services,IConfiguration configuration)
    {
        using var scope = services.CreateScope();

        var serviceProvider = scope.ServiceProvider;

        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await context.Database.MigrateAsync();

        await SeedRolesAsync(roleManager);

        await SeedAdminAsync(userManager,configuration);

        await CatalogSeeder.SeedAsync(context);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var roleName in ApplicationRoles.All)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole(roleName));

            if (!result.Succeeded)
            {
                var errors =string.Join(Environment.NewLine,
                    result.Errors.Select(
                            x =>
                                $"{x.Code}: {x.Description}"));

                throw new InvalidOperationException(
                    $"Failed to create role {roleName}:{Environment.NewLine}{errors}");
            }
        }
    }

    private static async Task SeedAdminAsync(UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        var email = configuration["SeedAdmin:Email"];

        var password = configuration["SeedAdmin:Password"];

        var fullName = configuration["SeedAdmin:FullName"] ?? "System Administrator";

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException(
                "SeedAdmin Email is missing.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "SeedAdmin Password is missing.");
        }

        var admin = await userManager.FindByEmailAsync(email);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                FullName = fullName,
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(admin,password);

            if (!createResult.Succeeded)
            {
                var errors = string.Join(Environment.NewLine,
                       createResult.Errors.Select(
                            x =>
                                $"{x.Code}: {x.Description}"));

                throw new InvalidOperationException(
                    $"Failed to create admin:{Environment.NewLine}{errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(admin,ApplicationRoles.Admin))
        {
            var roleResult = await userManager.AddToRoleAsync(admin,ApplicationRoles.Admin);

            if (!roleResult.Succeeded)
            {
                var errors =
                    string.Join(
                        Environment.NewLine,
                        roleResult.Errors.Select(
                            x =>
                                $"{x.Code}: {x.Description}"));

                throw new InvalidOperationException(
                    $"Failed to assign Admin role:{Environment.NewLine}{errors}");
            }
        }
    }
}