using ElectronicLibrary.BLL.Interfaces.UserManagement;
using ElectronicLibrary.DAL.Constants;
using ElectronicLibrary.DAL.Data;
using ElectronicLibrary.DAL.DTOs.Requests.UserManagement;
using ElectronicLibrary.DAL.DTOs.Responses.Common;
using ElectronicLibrary.DAL.DTOs.Responses.UserManagement;
using ElectronicLibrary.DAL.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ElectronicLibrary.BLL.Services.UserManagement;

public class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _dbContext;

    public UserManagementService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
    }

    public async Task<PagedResponse<UserSummaryResponse>> GetUsersAsync(
        UserQueryParameters parameters)
    {
        var query = _userManager.Users.AsNoTracking();

        if (!parameters.IncludeDeleted)
        {
            query = query.Where(user => !user.IsDeleted);
        }

        var search = parameters.Search?.Trim();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(user =>
                user.FullName.Contains(search) ||
                (user.Email != null && user.Email.Contains(search)));
        }

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderBy(user => user.FullName)
            .ThenBy(user => user.Email)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync();

        var items = new List<UserSummaryResponse>(users.Count);

        foreach (var user in users)
        {
            items.Add(await MapToSummaryResponseAsync(user));
        }

        return new PagedResponse<UserSummaryResponse>
        {
            Items = items,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    totalCount / (double)parameters.PageSize)
        };
    }

    public async Task<UserDetailsResponse> GetUserByIdAsync(
        string userId)
    {
        var user = await GetUserOrThrowAsync(userId);
        var roles = await _userManager.GetRolesAsync(user);

        return new UserDetailsResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            City = user.City,
            Address = user.Address,
            PhoneNumber = user.PhoneNumber,
            EmailConfirmed = user.EmailConfirmed,
            IsDeleted = user.IsDeleted,
            DeletedAt = user.DeletedAt,
            IsLockedOut = IsLockedOut(user),
            LockoutEnd = user.LockoutEnd,
            Roles = roles.ToList()
        };
    }

    public async Task<UserRolesResponse> GetUserRolesAsync(
        string userId)
    {
        var user = await GetUserOrThrowAsync(userId);

        return await MapToRolesResponseAsync(user);
    }

    public async Task<UserRolesResponse> AssignRoleAsync(
        string userId,
        AssignRoleRequest request)
    {
        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var user = await GetActiveUserOrThrowAsync(userId);
            var role = await GetValidRoleAsync(request.Role);

            if (await _userManager.IsInRoleAsync(user, role))
            {
                throw new InvalidOperationException(
                    "RoleAlreadyAssigned");
            }

            var addResult = await _userManager.AddToRoleAsync(
                user,
                role);

            if (!addResult.Succeeded)
            {
                throw new InvalidOperationException(
                    FormatIdentityErrors(addResult.Errors));
            }

            await InvalidateUserSessionsAsync(user);

            var response = await MapToRolesResponseAsync(user);

            await transaction.CommitAsync();

            return response;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<UserRolesResponse> RemoveRoleAsync(
        string actingAdminId,
        string userId,
        string role)
    {
        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var user = await GetActiveUserOrThrowAsync(userId);
            var validRole = await GetValidRoleAsync(role);
            var currentRoles = await _userManager.GetRolesAsync(user);

            if (!currentRoles.Contains(
                    validRole,
                    StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "RoleNotAssigned");
            }

            if (string.Equals(
                    validRole,
                    ApplicationRoles.Admin,
                    StringComparison.OrdinalIgnoreCase))
            {
                await ValidateAdminRoleRemovalAsync(
                    actingAdminId,
                    user);
            }

            if (currentRoles.Count == 1)
            {
                throw new InvalidOperationException(
                    "CannotRemoveOnlyRole");
            }

            var removeResult = await _userManager.RemoveFromRoleAsync(
                user,
                validRole);

            if (!removeResult.Succeeded)
            {
                throw new InvalidOperationException(
                    FormatIdentityErrors(removeResult.Errors));
            }

            await InvalidateUserSessionsAsync(user);

            var response = await MapToRolesResponseAsync(user);

            await transaction.CommitAsync();

            return response;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task ValidateAdminRoleRemovalAsync(
        string actingAdminId,
        ApplicationUser targetUser)
    {
        if (string.Equals(
                actingAdminId,
                targetUser.Id,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "CannotRemoveOwnAdminRole");
        }

        var administrators =
            await _userManager.GetUsersInRoleAsync(
                ApplicationRoles.Admin);

        var activeAdministratorCount = administrators.Count(
            administrator => !administrator.IsDeleted);

        if (activeAdministratorCount <= 1)
        {
            throw new InvalidOperationException(
                "CannotRemoveLastAdmin");
        }
    }

    private async Task<string> GetValidRoleAsync(string role)
    {
        if (string.IsNullOrWhiteSpace(role) || role.Length > 50)
        {
            throw new InvalidOperationException("InvalidRole");
        }

        var requestedRole = role.Trim();

        var validRole = ApplicationRoles.All.FirstOrDefault(
            applicationRole => string.Equals(
                applicationRole,
                requestedRole,
                StringComparison.OrdinalIgnoreCase));

        if (validRole is null ||
            !await _roleManager.RoleExistsAsync(validRole))
        {
            throw new InvalidOperationException("InvalidRole");
        }

        return validRole;
    }

    private async Task InvalidateUserSessionsAsync(
        ApplicationUser user)
    {
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

    private async Task<UserSummaryResponse> MapToSummaryResponseAsync(
        ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        return new UserSummaryResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            City = user.City,
            EmailConfirmed = user.EmailConfirmed,
            IsDeleted = user.IsDeleted,
            DeletedAt = user.DeletedAt,
            IsLockedOut = IsLockedOut(user),
            LockoutEnd = user.LockoutEnd,
            Roles = roles.ToList()
        };
    }

    private async Task<UserRolesResponse> MapToRolesResponseAsync(
        ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        return new UserRolesResponse
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            Roles = roles.ToList()
        };
    }

    private async Task<ApplicationUser> GetActiveUserOrThrowAsync(
        string userId)
    {
        var user = await GetUserOrThrowAsync(userId);

        if (user.IsDeleted)
        {
            throw new InvalidOperationException(
                "CannotModifyDeletedUser");
        }

        return user;
    }

    private async Task<ApplicationUser> GetUserOrThrowAsync(
        string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new KeyNotFoundException("UserNotFound");
        }

        var user = await _userManager.FindByIdAsync(userId.Trim());

        if (user is null)
        {
            throw new KeyNotFoundException("UserNotFound");
        }

        return user;
    }

    private static bool IsLockedOut(ApplicationUser user)
    {
        return user.LockoutEnd.HasValue &&
               user.LockoutEnd.Value > DateTimeOffset.UtcNow;
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
