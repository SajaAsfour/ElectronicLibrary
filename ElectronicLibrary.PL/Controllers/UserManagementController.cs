using ElectronicLibrary.BLL.Interfaces.UserManagement;
using ElectronicLibrary.DAL.DTOs.Requests.UserManagement;
using ElectronicLibrary.DAL.DTOs.Responses.Common;
using ElectronicLibrary.DAL.DTOs.Responses.UserManagement;
using ElectronicLibrary.PL.Authorization;
using ElectronicLibrary.PL.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace ElectronicLibrary.PL.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
public class UserManagementController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;
    private readonly IStringLocalizer<SharedResources> _localizer;

    public UserManagementController(
        IUserManagementService userManagementService,
        IStringLocalizer<SharedResources> localizer)
    {
        _userManagementService = userManagementService;
        _localizer = localizer;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResponse<UserSummaryResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<UserSummaryResponse>>>
        GetUsers([FromQuery] UserQueryParameters parameters)
    {
        var response = await _userManagementService.GetUsersAsync(
            parameters);

        return Ok(response);
    }

    [HttpGet("{userId}")]
    [ProducesResponseType(
        typeof(UserDetailsResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDetailsResponse>> GetUserById(
        string userId)
    {
        var response =
            await _userManagementService.GetUserByIdAsync(userId);

        return Ok(response);
    }

    [HttpGet("{userId}/roles")]
    [ProducesResponseType(
        typeof(UserRolesResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserRolesResponse>> GetUserRoles(
        string userId)
    {
        var response =
            await _userManagementService.GetUserRolesAsync(userId);

        return Ok(response);
    }

    [HttpPost("{userId}/roles")]
    [ProducesResponseType(
        typeof(UserRoleUpdateResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserRoleUpdateResponse>> AssignRole(
        string userId,
        [FromBody] AssignRoleRequest request)
    {
        var roles = await _userManagementService.AssignRoleAsync(
            userId,
            request);

        return Ok(CreateRoleUpdateResponse(
            roles,
            "RoleAssignedSuccessfully"));
    }

    [HttpDelete("{userId}/roles/{role}")]
    [ProducesResponseType(
        typeof(UserRoleUpdateResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserRoleUpdateResponse>> RemoveRole(
        string userId,
        string role)
    {
        var actingAdminId = GetAuthenticatedUserId();

        var roles = await _userManagementService.RemoveRoleAsync(
            actingAdminId,
            userId,
            role);

        return Ok(CreateRoleUpdateResponse(
            roles,
            "RoleRemovedSuccessfully"));
    }

    private UserRoleUpdateResponse CreateRoleUpdateResponse(
        UserRolesResponse roles,
        string messageKey)
    {
        return new UserRoleUpdateResponse
        {
            Message = _localizer[messageKey].Value,
            UserId = roles.UserId,
            Email = roles.Email,
            Roles = roles.Roles
        };
    }

    private string GetAuthenticatedUserId()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException(
                "InvalidAccessToken");
        }

        return userId;
    }
}
