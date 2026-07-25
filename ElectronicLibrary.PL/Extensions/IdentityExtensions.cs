using ElectronicLibrary.DAL.Data;
using ElectronicLibrary.DAL.Models.Identity;
using Microsoft.AspNetCore.Identity;

namespace ElectronicLibrary.PL.Extensions;

public static class IdentityExtensions
{
    public static IServiceCollection AddIdentityServices(
        this IServiceCollection services)
    {
        services
            .AddIdentity<ApplicationUser, IdentityRole>(
                options =>
                {
                    options.User.RequireUniqueEmail = true;

                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = true;
                    options.Password.RequiredLength = 8;

                    options.Lockout.MaxFailedAccessAttempts = 5;

                    options.Lockout.DefaultLockoutTimeSpan =
                        TimeSpan.FromMinutes(15);
                })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }
}