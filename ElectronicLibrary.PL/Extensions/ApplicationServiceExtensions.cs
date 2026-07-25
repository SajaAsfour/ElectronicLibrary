using ElectronicLibrary.BLL.Interfaces.Authentication;
using ElectronicLibrary.BLL.Services.Authentication;

namespace ElectronicLibrary.PL.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped<ITokenService,TokenService>();

        return services;
    }
}