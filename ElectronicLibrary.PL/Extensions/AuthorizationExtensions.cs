using ElectronicLibrary.DAL.Constants;
using ElectronicLibrary.PL.Authorization;

namespace ElectronicLibrary.PL.Extensions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddAuthorizationPolicies(
        this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AuthorizationPolicyNames.AdminOnly,
                policy => policy
                    .RequireAuthenticatedUser()
                    .RequireRole(ApplicationRoles.Admin));
        });

        return services;
    }
}
