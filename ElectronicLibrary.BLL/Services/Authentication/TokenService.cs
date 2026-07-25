using ElectronicLibrary.BLL.Interfaces.Authentication;
using ElectronicLibrary.BLL.Options;
using ElectronicLibrary.DAL.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ElectronicLibrary.BLL.Services.Authentication;

public class TokenService : ITokenService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtOptions _jwtOptions;

    public TokenService(UserManager<ApplicationUser> userManager,IOptions<JwtOptions> jwtOptions)
    {
        _userManager = userManager;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<string> CreateAccessTokenAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                user.Id),

            new(
                ClaimTypes.Name,
                user.UserName ?? string.Empty),

            new(
                ClaimTypes.Email,
                user.Email ?? string.Empty),

            new(
                "FullName",
                user.FullName),

            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        };

        claims.AddRange(
            roles.Select(role =>
                new Claim(
                    ClaimTypes.Role,
                    role)));

        var key =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _jwtOptions.SecretKey));

        var credentials =
            new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

        var token =
            new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: GetAccessTokenExpirationTime(),
                signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string CreateRefreshToken()
    {
        var randomBytes = new byte[64];

        using var randomNumberGenerator = RandomNumberGenerator.Create();

        randomNumberGenerator.GetBytes(randomBytes);

        return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal GetPrincipalFromExpiredToken(string accessToken)
    {
        var tokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _jwtOptions.Issuer,

                ValidateAudience = true,
                ValidAudience = _jwtOptions.Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            _jwtOptions.SecretKey)),

                ValidateLifetime = false,

                ClockSkew = TimeSpan.Zero
            };

        var tokenHandler = new JwtSecurityTokenHandler();

        var principal =
            tokenHandler.ValidateToken(
                accessToken,
                tokenValidationParameters,
                out var securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(
                SecurityAlgorithms.HmacSha256,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new SecurityTokenException(
                "Invalid access token.");
        }

        return principal;
    }

    public DateTime GetAccessTokenExpirationTime()
    {
        return DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes);
    }

    public DateTime GetRefreshTokenExpirationTime()
    {
        return DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays);
    }
}