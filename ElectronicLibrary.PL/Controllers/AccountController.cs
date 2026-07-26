using ElectronicLibrary.BLL.Interfaces.Authentication;
using ElectronicLibrary.DAL.DTOs.Requests.Authentication;
using ElectronicLibrary.DAL.DTOs.Responses.Authentication;
using ElectronicLibrary.DAL.DTOs.Responses.Common;
using ElectronicLibrary.PL.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace ElectronicLibrary.PL.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IStringLocalizer<SharedResources> _localizer;

    public AccountController(
        IAuthenticationService authenticationService,
        IStringLocalizer<SharedResources> localizer)
    {
        _authenticationService = authenticationService;
        _localizer = localizer;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(RegisterResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<RegisterResponse>> Register(
        RegisterRequest request)
    {
        var response = await _authenticationService.RegisterAsync(
            request);

        response.Message =
            _localizer["RegistrationPendingEmailConfirmation"].Value;

        return StatusCode(
            StatusCodes.Status201Created,
            response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(AuthenticationResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthenticationResponse>> Login(
        LoginRequest request)
    {
        var response = await _authenticationService.LoginAsync(
            request);

        return Ok(response);
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(AuthenticationResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthenticationResponse>> RefreshToken(
        RefreshTokenRequest request)
    {
        var response =
            await _authenticationService.RefreshTokenAsync(request);

        return Ok(response);
    }

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(MessageResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MessageResponse>> ConfirmEmail(
        ConfirmEmailRequest request)
    {
        await _authenticationService.ConfirmEmailAsync(request);

        return Ok(new MessageResponse
        {
            Message = _localizer["EmailConfirmedSuccessfully"].Value
        });
    }

    [HttpGet("confirm-email")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(MessageResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MessageResponse>> ConfirmEmailFromLink(
        [FromQuery] ConfirmEmailRequest request)
    {
        await _authenticationService.ConfirmEmailAsync(request);

        return Ok(new MessageResponse
        {
            Message = _localizer["EmailConfirmedSuccessfully"].Value
        });
    }

    [HttpPost("resend-confirmation-email")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(MessageResponse),
        StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<MessageResponse>>
        ResendConfirmationEmail(
            ResendConfirmationEmailRequest request)
    {
        await _authenticationService.ResendConfirmationEmailAsync(
            request);

        return Accepted(new MessageResponse
        {
            Message = _localizer[
                "ConfirmationEmailRequestAccepted"].Value
        });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(MessageResponse),
        StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<MessageResponse>> ForgotPassword(
        ForgotPasswordRequest request)
    {
        await _authenticationService.ForgotPasswordAsync(request);

        return Accepted(new MessageResponse
        {
            Message = _localizer[
                "PasswordResetRequestAccepted"].Value
        });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(MessageResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MessageResponse>> ResetPassword(
        ResetPasswordRequest request)
    {
        await _authenticationService.ResetPasswordAsync(request);

        return Ok(new MessageResponse
        {
            Message = _localizer[
                "PasswordResetSuccessful"].Value
        });
    }

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(
        typeof(MessageResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageResponse>> ChangePassword(
        ChangePasswordRequest request)
    {
        var userId = GetAuthenticatedUserId();

        await _authenticationService.ChangePasswordAsync(
            userId,
            request);

        return Ok(new MessageResponse
        {
            Message = _localizer[
                "PasswordChangedSuccessfully"].Value
        });
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(
        typeof(CurrentUserResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CurrentUserResponse>>
        GetCurrentUser()
    {
        var userId = GetAuthenticatedUserId();

        var response =
            await _authenticationService.GetCurrentUserAsync(
                userId);

        return Ok(response);
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        var userId = GetAuthenticatedUserId();

        await _authenticationService.LogoutAsync(userId);

        return NoContent();
    }
    private string GetAuthenticatedUserId()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException(
                "UnauthorizedRequest");
        }

        return userId;
    }

}
