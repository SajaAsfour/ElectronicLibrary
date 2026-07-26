using ElectronicLibrary.BLL.Interfaces.Authentication;
using ElectronicLibrary.BLL.Interfaces.Email;
using ElectronicLibrary.DAL.Constants;
using ElectronicLibrary.DAL.DTOs.Requests.Authentication;
using ElectronicLibrary.DAL.DTOs.Responses.Authentication;
using ElectronicLibrary.DAL.Models.Identity;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ElectronicLibrary.BLL.Services.Authentication;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        IEmailService emailService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _emailService = emailService;
    }

    public async Task<RegisterResponse> RegisterAsync(
        RegisterRequest request)
    {
        var email = request.Email.Trim();

        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            throw new InvalidOperationException(
                "UserAlreadyExists");
        }

        var user = new ApplicationUser
        {
            FullName = request.FullName.Trim(),
            Email = email,
            UserName = email,
            City = request.City?.Trim(),
            Address = request.Address?.Trim(),
            EmailConfirmed = false,
            LockoutEnabled = true
        };

        var createResult = await _userManager.CreateAsync(
            user,
            request.Password);

        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                FormatIdentityErrors(createResult.Errors));
        }

        var roleResult = await _userManager.AddToRoleAsync(
            user,
            ApplicationRoles.Customer);

        if (!roleResult.Succeeded)
        {
            var deleteResult = await _userManager.DeleteAsync(user);

            var errors = roleResult.Errors.ToList();

            if (!deleteResult.Succeeded)
            {
                errors.AddRange(deleteResult.Errors);
            }

            throw new InvalidOperationException(
                FormatIdentityErrors(errors));
        }

        await SendConfirmationEmailAsync(user);

        return new RegisterResponse
        {
            UserId = user.Id,
            Email = user.Email ?? email,
            EmailConfirmationRequired = true
        };
    }

    public async Task<AuthenticationResponse> LoginAsync(
        LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(
            request.Email.Trim());

        if (user is null || user.IsDeleted)
        {
            throw new UnauthorizedAccessException(
                "InvalidCredentials");
        }

        if (!await _userManager.IsEmailConfirmedAsync(user))
        {
            throw new UnauthorizedAccessException(
                "EmailNotConfirmed");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            throw new UnauthorizedAccessException(
                "AccountLocked");
        }

        var signInResult =
            await _signInManager.CheckPasswordSignInAsync(
                user,
                request.Password,
                lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
        {
            throw new UnauthorizedAccessException(
                "AccountLocked");
        }

        if (signInResult.IsNotAllowed)
        {
            throw new UnauthorizedAccessException(
                "EmailNotConfirmed");
        }

        if (!signInResult.Succeeded)
        {
            throw new UnauthorizedAccessException(
                "InvalidCredentials");
        }

        return await CreateAuthenticationResponseAsync(user);
    }

    public async Task<AuthenticationResponse> RefreshTokenAsync(
        RefreshTokenRequest request)
    {
        ClaimsPrincipal principal;

        try
        {
            principal = _tokenService.GetPrincipalFromExpiredToken(
                request.AccessToken);
        }
        catch
        {
            throw new UnauthorizedAccessException(
                "InvalidAccessToken");
        }

        var userId = principal.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException(
                "InvalidAccessToken");
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user is null || user.IsDeleted)
        {
            throw new UnauthorizedAccessException(
                "InvalidRefreshToken");
        }

        if (!await _userManager.IsEmailConfirmedAsync(user))
        {
            throw new UnauthorizedAccessException(
                "EmailNotConfirmed");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            throw new UnauthorizedAccessException(
                "AccountLocked");
        }

        if (string.IsNullOrWhiteSpace(user.RefreshTokenHash))
        {
            throw new UnauthorizedAccessException(
                "RefreshTokenNotAvailable");
        }

        if (user.RefreshTokenExpiryTime is null ||
            user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException(
                "RefreshTokenExpired");
        }

        var receivedRefreshTokenHash = HashRefreshToken(
            request.RefreshToken);

        byte[] storedHash;
        byte[] receivedHash;

        try
        {
            storedHash = Convert.FromBase64String(
                user.RefreshTokenHash);

            receivedHash = Convert.FromBase64String(
                receivedRefreshTokenHash);
        }
        catch (FormatException)
        {
            throw new UnauthorizedAccessException(
                "InvalidRefreshToken");
        }

        if (!CryptographicOperations.FixedTimeEquals(
                storedHash,
                receivedHash))
        {
            throw new UnauthorizedAccessException(
                "InvalidRefreshToken");
        }

        return await CreateAuthenticationResponseAsync(user);
    }

    public async Task ConfirmEmailAsync(
        ConfirmEmailRequest request)
    {
        var user = await _userManager.FindByIdAsync(
            request.UserId.Trim());

        if (user is null || user.IsDeleted)
        {
            throw new InvalidOperationException(
                "InvalidEmailConfirmationToken");
        }

        if (await _userManager.IsEmailConfirmedAsync(user))
        {
            return;
        }

        var result = await _userManager.ConfirmEmailAsync(
            user,
            request.Token);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "InvalidEmailConfirmationToken");
        }
    }

    public async Task ResendConfirmationEmailAsync(
        ResendConfirmationEmailRequest request)
    {
        var user = await _userManager.FindByEmailAsync(
            request.Email.Trim());

        if (user is null ||
            user.IsDeleted ||
            await _userManager.IsEmailConfirmedAsync(user))
        {
            return;
        }

        await SendConfirmationEmailAsync(user);
    }

    public async Task LogoutAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            throw new KeyNotFoundException(
                "UserNotFound");
        }

        user.RefreshTokenHash = null;
        user.RefreshTokenExpiryTime = null;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                FormatIdentityErrors(result.Errors));
        }
    }

    private async Task SendConfirmationEmailAsync(
        ApplicationUser user)
    {
        var confirmationToken =
            await _userManager.GenerateEmailConfirmationTokenAsync(user);

        await _emailService.SendEmailConfirmationAsync(
            user.Email ?? string.Empty,
            user.FullName,
            user.Id,
            confirmationToken);
    }

    private async Task<AuthenticationResponse>
        CreateAuthenticationResponseAsync(ApplicationUser user)
    {
        var accessTokenExpiration =
            _tokenService.GetAccessTokenExpirationTime();

        var refreshTokenExpiration =
            _tokenService.GetRefreshTokenExpirationTime();

        var accessToken = await _tokenService.CreateAccessTokenAsync(user);

        var refreshToken = _tokenService.CreateRefreshToken();

        user.RefreshTokenHash = HashRefreshToken(refreshToken);
        user.RefreshTokenExpiryTime = refreshTokenExpiration;

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException(
                FormatIdentityErrors(updateResult.Errors));
        }

        var roles = await _userManager.GetRolesAsync(user);

        return new AuthenticationResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = accessTokenExpiration,
            RefreshTokenExpiresAt = refreshTokenExpiration,
            Roles = roles.ToList()
        };
    }

    private static string HashRefreshToken(string refreshToken)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(refreshToken);
        var hashBytes = SHA256.HashData(tokenBytes);

        return Convert.ToBase64String(hashBytes);
    }

    private static string FormatIdentityErrors(IEnumerable<IdentityError> errors)
    {
        return string.Join(
            Environment.NewLine,
            errors.Select(error =>
                $"{error.Code}: {error.Description}"));
    }
}