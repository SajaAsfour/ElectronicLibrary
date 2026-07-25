using ElectronicLibrary.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicLibrary.PL.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabaseServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection is not configured.");

        services.AddDbContext<ApplicationDbContext>(
            options =>
                options.UseSqlServer(connectionString));

        return services;
    }
}