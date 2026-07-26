using ElectronicLibrary.DAL.DTOs.Requests.Authentication;
using ElectronicLibrary.DAL.DTOs.Responses.Authentication;

namespace ElectronicLibrary.BLL.Interfaces.Authentication;

public interface IAuthenticationService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);

    Task<AuthenticationResponse> LoginAsync(LoginRequest request);

    Task<AuthenticationResponse> RefreshTokenAsync(RefreshTokenRequest request);

    Task RevokeRefreshTokenAsync(RefreshTokenRequest request);

    Task ConfirmEmailAsync(ConfirmEmailRequest request);

    Task ResendConfirmationEmailAsync(
        ResendConfirmationEmailRequest request);

    Task ForgotPasswordAsync(ForgotPasswordRequest request);

    Task ResetPasswordAsync(ResetPasswordRequest request);

    Task ChangePasswordAsync(
        string userId,
        ChangePasswordRequest request);

    Task<CurrentUserResponse> GetCurrentUserAsync(string userId);

    Task LogoutAsync(string userId);
}