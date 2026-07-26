using ElectronicLibrary.DAL.DTOs.Requests.UserManagement;
using ElectronicLibrary.DAL.DTOs.Responses.Common;
using ElectronicLibrary.DAL.DTOs.Responses.UserManagement;

namespace ElectronicLibrary.BLL.Interfaces.UserManagement;

public interface IUserManagementService
{
    Task<PagedResponse<UserSummaryResponse>> GetUsersAsync(
        UserQueryParameters parameters);

    Task<UserDetailsResponse> GetUserByIdAsync(
        string userId);

    Task<UserRolesResponse> GetUserRolesAsync(
        string userId);

    Task<UserRolesResponse> AssignRoleAsync(
        string userId,
        AssignRoleRequest request);

    Task<UserRolesResponse> RemoveRoleAsync(
        string actingAdminId,
        string userId,
        string role);
}
