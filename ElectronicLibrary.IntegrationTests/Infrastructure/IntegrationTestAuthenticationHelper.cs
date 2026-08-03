using ElectronicLibrary.BLL.Interfaces.Authentication;
using ElectronicLibrary.DAL.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;

namespace ElectronicLibrary.IntegrationTests.Infrastructure;

public static class IntegrationTestAuthenticationHelper
{
    private const string TestPassword =
        "IntegrationTest@123";

    public static async Task<string> CreateAccessTokenAsync(
        CustomWebApplicationFactory factory,
        string role)
    {
        using IServiceScope scope =
            factory.Services.CreateScope();

        IServiceProvider services =
            scope.ServiceProvider;

        var roleManager =
            services.GetRequiredService<
                RoleManager<IdentityRole>>();

        var userManager =
            services.GetRequiredService<
                UserManager<ApplicationUser>>();

        var tokenService =
            services.GetRequiredService<ITokenService>();

        if (!await roleManager.RoleExistsAsync(role))
        {
            IdentityResult roleResult =
                await roleManager.CreateAsync(
                    new IdentityRole(role));

            EnsureSucceeded(
                roleResult,
                $"creating role '{role}'");
        }

        string email =
            $"{role.ToLowerInvariant()}" +
            "@integrationtests.local";

        ApplicationUser? user =
            await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                FullName =
                    $"{role} Integration Test User",
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            IdentityResult createResult =
                await userManager.CreateAsync(
                    user,
                    TestPassword);

            EnsureSucceeded(
                createResult,
                $"creating test user '{email}'");
        }

        if (!await userManager.IsInRoleAsync(
                user,
                role))
        {
            IdentityResult roleAssignmentResult =
                await userManager.AddToRoleAsync(
                    user,
                    role);

            EnsureSucceeded(
                roleAssignmentResult,
                $"assigning role '{role}'");
        }

        return await tokenService
            .CreateAccessTokenAsync(user);
    }

    public static void SetBearerToken(
        HttpClient client,
        string accessToken)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);
    }

    public static void ClearBearerToken(
        HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization =
            null;
    }

    private static void EnsureSucceeded(
        IdentityResult result,
        string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        string errors = string.Join(
            Environment.NewLine,
            result.Errors.Select(
                error =>
                    $"{error.Code}: " +
                    error.Description));

        throw new InvalidOperationException(
            $"Integration test setup failed while " +
            $"{operation}:{Environment.NewLine}" +
            errors);
    }
}