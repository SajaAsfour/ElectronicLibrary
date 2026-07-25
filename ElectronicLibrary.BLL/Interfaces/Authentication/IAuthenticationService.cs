using ElectronicLibrary.DAL.DTOs.Requests.Authentication;
using ElectronicLibrary.DAL.DTOs.Responses;
using ElectronicLibrary.DAL.DTOs.Responses.Authentication;

namespace ElectronicLibrary.BLL.Interfaces.Authentication;

public interface IAuthenticationService
{
    Task<AuthenticationResponse> RegisterAsync(RegisterRequest request);
    Task<AuthenticationResponse> LoginAsync(LoginRequest request);
    Task<AuthenticationResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task LogoutAsync(string userId);
}