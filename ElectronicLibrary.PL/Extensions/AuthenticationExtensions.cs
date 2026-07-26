using ElectronicLibrary.BLL.Options;
using ElectronicLibrary.DAL.Constants;
using ElectronicLibrary.DAL.Models.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace ElectronicLibrary.PL.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthenticationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection =
            configuration.GetSection(JwtOptions.SectionName);

        var jwtOptions =
            jwtSection.Get<JwtOptions>()
            ?? throw new InvalidOperationException(
                "JWT configuration is missing.");

        if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey))
        {
            throw new InvalidOperationException(
                "JWT SecretKey is missing.");
        }

        if (string.IsNullOrWhiteSpace(jwtOptions.Issuer))
        {
            throw new InvalidOperationException(
                "JWT Issuer is missing.");
        }

        if (string.IsNullOrWhiteSpace(jwtOptions.Audience))
        {
            throw new InvalidOperationException(
                "JWT Audience is missing.");
        }

        services.Configure<JwtOptions>(jwtSection);

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultScheme =
                    JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtOptions.Issuer,

                        ValidateAudience = true,
                        ValidAudience = jwtOptions.Audience,

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey =
                            new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(
                                    jwtOptions.SecretKey)),

                        ValidateLifetime = true,

                        ClockSkew = TimeSpan.Zero
                    };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var userId = context.Principal?.FindFirstValue(
                            ClaimTypes.NameIdentifier);

                        if (string.IsNullOrWhiteSpace(userId))
                        {
                            context.Fail("Invalid user token.");
                            return;
                        }

                        var userManager = context.HttpContext
                            .RequestServices
                            .GetRequiredService<
                                UserManager<ApplicationUser>>();

                        var user = await userManager.FindByIdAsync(userId);

                        if (user is null || user.IsDeleted)
                        {
                            context.Fail(
                                "The user account is not active.");
                            return;
                        }

                        var tokenSecurityStamp = context.Principal
                            ?.FindFirstValue(
                                CustomClaimTypes.SecurityStamp);

                        if (string.IsNullOrWhiteSpace(
                                tokenSecurityStamp) ||
                            !string.Equals(
                                tokenSecurityStamp,
                                user.SecurityStamp,
                                StringComparison.Ordinal))
                        {
                            context.Fail(
                                "The access token is no longer valid.");
                        }
                    }
                };
            });

        return services;
    }
}