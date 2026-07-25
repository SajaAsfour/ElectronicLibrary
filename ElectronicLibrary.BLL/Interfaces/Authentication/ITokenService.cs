using ElectronicLibrary.DAL.Models.Identity;
using System.Security.Claims;

namespace ElectronicLibrary.BLL.Interfaces.Authentication;

public interface ITokenService
{
    Task<string> CreateAccessTokenAsync(ApplicationUser user);

    string CreateRefreshToken();

    ClaimsPrincipal GetPrincipalFromExpiredToken(string accessToken);

    DateTime GetAccessTokenExpirationTime();

    DateTime GetRefreshTokenExpirationTime();
}