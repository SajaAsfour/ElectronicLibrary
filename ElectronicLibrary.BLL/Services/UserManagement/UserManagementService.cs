using ElectronicLibrary.BLL.Interfaces.UserManagement;
using ElectronicLibrary.DAL.DTOs.Requests.UserManagement;
using ElectronicLibrary.DAL.DTOs.Responses.Common;
using ElectronicLibrary.DAL.DTOs.Responses.UserManagement;
using ElectronicLibrary.DAL.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ElectronicLibrary.BLL.Services.UserManagement;

public class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserManagementService(
        UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
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
        var roles = await _userManager.GetRolesAsync(user);

        return new UserRolesResponse
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            Roles = roles.ToList()
        };
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
}
