using ElectronicLibrary.BLL.Interfaces.Authentication;
using ElectronicLibrary.BLL.Interfaces.UserManagement;
using ElectronicLibrary.BLL.Services.Authentication;
using ElectronicLibrary.BLL.Services.UserManagement;
using ElectronicLibrary.DAL.Repositories.Generic;
using ElectronicLibrary.DAL.Repositories.UnitOfWork;

namespace ElectronicLibrary.PL.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped<ITokenService, TokenService>();

        services.AddScoped<IAuthenticationService, AuthenticationService>();

        services.AddScoped<IUserManagementService, UserManagementService>();

        services.AddScoped(typeof(IGenericRepository<>),typeof(GenericRepository<>));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}