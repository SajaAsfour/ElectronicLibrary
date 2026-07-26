using ElectronicLibrary.BLL.Interfaces.UserManagement;
using ElectronicLibrary.DAL.DTOs.Requests.UserManagement;
using ElectronicLibrary.DAL.DTOs.Responses.Common;
using ElectronicLibrary.DAL.DTOs.Responses.UserManagement;
using ElectronicLibrary.PL.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicLibrary.PL.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = AuthorizationPolicyNames.AdminOnly)]
public class UserManagementController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public UserManagementController(
        IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
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
}
