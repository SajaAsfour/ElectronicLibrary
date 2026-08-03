using ElectronicLibrary.BLL.Interfaces.Authentication;
using ElectronicLibrary.BLL.Interfaces.Email;
using ElectronicLibrary.DAL.Constants;
using ElectronicLibrary.DAL.DTOs.Requests.Authentication;
using ElectronicLibrary.DAL.DTOs.Responses.Authentication;
using ElectronicLibrary.DAL.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
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
        var user = await GetUserByValidRefreshTokenAsync(request);

        return await CreateAuthenticationResponseAsync(user);
    }

    public async Task RevokeRefreshTokenAsync(
        RefreshTokenRequest request)
    {
        var user = await GetUserByValidRefreshTokenAsync(request);

        user.RefreshTokenHash = null;
        user.RefreshTokenExpiryTime = null;

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException(
                FormatIdentityErrors(updateResult.Errors));
        }
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

        if (!TryDecodeIdentityToken(
                request.Token,
                out var decodedToken))
        {
            throw new InvalidOperationException(
                "InvalidEmailConfirmationToken");
        }

        var result = await _userManager.ConfirmEmailAsync(
            user,
            decodedToken);

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

    public async Task ForgotPasswordAsync(
        ForgotPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(
            request.Email.Trim());

        if (user is null ||
            user.IsDeleted ||
            !await _userManager.IsEmailConfirmedAsync(user))
        {
            return;
        }

        var resetToken =
            await _userManager.GeneratePasswordResetTokenAsync(user);

        await _emailService.SendPasswordResetAsync(
            user.Email ?? string.Empty,
            user.FullName,
            user.Id,
            resetToken);
    }

    public async Task ResetPasswordAsync(
        ResetPasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(
            request.UserId.Trim());

        if (user is null || user.IsDeleted)
        {
            throw new InvalidOperationException(
                "InvalidPasswordResetToken");
        }

        if (!TryDecodeIdentityToken(
                request.Token,
                out var decodedToken))
        {
            throw new InvalidOperationException(
                "InvalidPasswordResetToken");
        }

        var resetResult = await _userManager.ResetPasswordAsync(
            user,
            decodedToken,
            request.NewPassword);

        if (!resetResult.Succeeded)
        {
            if (resetResult.Errors.Any(error =>
                    string.Equals(
                        error.Code,
                        "InvalidToken",
                        StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    "InvalidPasswordResetToken");
            }

            throw new InvalidOperationException(
                FormatIdentityErrors(resetResult.Errors));
        }

        user.RefreshTokenHash = null;
        user.RefreshTokenExpiryTime = null;
        user.AccessFailedCount = 0;
        user.LockoutEnd = null;

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException(
                FormatIdentityErrors(updateResult.Errors));
        }
    }

    public async Task ChangePasswordAsync(
        string userId,
        ChangePasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null || user.IsDeleted)
        {
            throw new KeyNotFoundException(
                "UserNotFound");
        }

        var changePasswordResult =
            await _userManager.ChangePasswordAsync(
                user,
                request.CurrentPassword,
                request.NewPassword);

        if (!changePasswordResult.Succeeded)
        {
            if (changePasswordResult.Errors.Any(error =>
                    string.Equals(
                        error.Code,
                        "PasswordMismatch",
                        StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    "CurrentPasswordIncorrect");
            }

            throw new InvalidOperationException(
                FormatIdentityErrors(
                    changePasswordResult.Errors));
        }

        user.RefreshTokenHash = null;
        user.RefreshTokenExpiryTime = null;

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException(
                FormatIdentityErrors(updateResult.Errors));
        }
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync(
        string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null || user.IsDeleted)
        {
            throw new KeyNotFoundException(
                "UserNotFound");
        }

        var roles = await _userManager.GetRolesAsync(user);

        return new CurrentUserResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            City = user.City,
            Address = user.Address,
            StoreName = user.StoreName,
            SellerBio = user.SellerBio,
            SellerRating = user.SellerRating,
            EmailConfirmed = user.EmailConfirmed,
            Roles = roles.ToList()
        };
    }

    public async Task UpdateProfileAsync(
        string userId,
        UpdateProfileRequest request)
    {
        var user = await GetActiveUserAsync(userId);

        user.FullName = request.FullName.Trim();

        await UpdateUserAsync(user);
    }

    public async Task UpdateSellerProfileAsync(
        string userId,
        UpdateSellerProfileRequest request)
    {
        var user = await GetActiveUserAsync(userId);

        if (!await _userManager.IsInRoleAsync(
                user,
                ApplicationRoles.Seller))
        {
            throw new UnauthorizedAccessException(
                "SellerRoleRequired");
        }

        user.StoreName = request.StoreName.Trim();
        user.SellerBio = NormalizeOptionalValue(request.Bio);

        await UpdateUserAsync(user);
    }

    public async Task UpdateAddressAsync(
        string userId,
        UpdateAddressRequest request)
    {
        var user = await GetActiveUserAsync(userId);

        user.Address = NormalizeOptionalValue(request.Address);

        await UpdateUserAsync(user);
    }

    public async Task UpdateCityAsync(
        string userId,
        UpdateCityRequest request)
    {
        var user = await GetActiveUserAsync(userId);

        user.City = NormalizeOptionalValue(request.City);

        await UpdateUserAsync(user);
    }

    public async Task DeleteAccountAsync(
        string userId,
        DeleteAccountRequest request)
    {
        var user = await GetActiveUserAsync(userId);

        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Contains(
                ApplicationRoles.Admin,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "AdminAccountCannotBeDeleted");
        }

        var passwordIsValid = await _userManager.CheckPasswordAsync(
            user,
            request.CurrentPassword);

        if (!passwordIsValid)
        {
            throw new InvalidOperationException(
                "CurrentPasswordIncorrect");
        }

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.RefreshTokenHash = null;
        user.RefreshTokenExpiryTime = null;

        var updateResult =
            await _userManager.UpdateSecurityStampAsync(user);

        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException(
                FormatIdentityErrors(updateResult.Errors));
        }
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

    private async Task<ApplicationUser> GetActiveUserAsync(
        string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null || user.IsDeleted)
        {
            throw new KeyNotFoundException(
                "UserNotFound");
        }

        return user;
    }

    private async Task UpdateUserAsync(ApplicationUser user)
    {
        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException(
                FormatIdentityErrors(updateResult.Errors));
        }
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private async Task<ApplicationUser>
        GetUserByValidRefreshTokenAsync(
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

        var tokenSecurityStamp = principal.FindFirstValue(
            CustomClaimTypes.SecurityStamp);

        if (string.IsNullOrWhiteSpace(tokenSecurityStamp) ||
            !string.Equals(
                tokenSecurityStamp,
                user.SecurityStamp,
                StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException(
                "InvalidAccessToken");
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

        return user;
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

        var accessToken =
            await _tokenService.CreateAccessTokenAsync(user);

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

    private static bool TryDecodeIdentityToken(
        string encodedToken,
        out string decodedToken)
    {
        decodedToken = string.Empty;

        try
        {
            var tokenBytes = WebEncoders.Base64UrlDecode(
                encodedToken.Trim());

            decodedToken = Encoding.UTF8.GetString(tokenBytes);

            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string HashRefreshToken(
        string refreshToken)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(refreshToken);
        var hashBytes = SHA256.HashData(tokenBytes);

        return Convert.ToBase64String(hashBytes);
    }

    private static string FormatIdentityErrors(
        IEnumerable<IdentityError> errors)
    {
        return string.Join(
            Environment.NewLine,
            errors.Select(error =>
                $"{error.Code}: {error.Description}"));
    }
}
