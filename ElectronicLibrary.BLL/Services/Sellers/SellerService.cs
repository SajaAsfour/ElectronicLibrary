using ElectronicLibrary.BLL.Exceptions;
using ElectronicLibrary.BLL.Interfaces.Common;
using ElectronicLibrary.BLL.Interfaces.Sellers;
using ElectronicLibrary.DAL.Constants;
using ElectronicLibrary.DAL.DTOs.Requests.Sellers;
using ElectronicLibrary.DAL.DTOs.Responses.Sellers;
using ElectronicLibrary.DAL.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ElectronicLibrary.BLL.Services.Sellers;

public class SellerService : ISellerService
{
    private const int MaximumStoreNameLength = 150;
    private const int MaximumSellerBioLength = 1000;
    private const int MaximumCityLength = 100;
    private const int MaximumAddressLength = 500;

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUserService;

    public SellerService(
        UserManager<ApplicationUser> userManager,
        ICurrentUserService currentUserService)
    {
        _userManager = userManager;
        _currentUserService = currentUserService;
    }

    public async Task<SellerProfileResponse> ActivateSellerAsync(
        ActivateSellerRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ApplicationUser user =
            await GetCurrentActiveUserAsync(cancellationToken);

        bool isAlreadySeller = await _userManager.IsInRoleAsync(
            user,
            ApplicationRoles.Seller);

        if (isAlreadySeller)
        {
            throw new ConflictException(
                "SellerProfileAlreadyActivated");
        }

        string normalizedStoreName =
            NormalizeAndValidateStoreName(request.StoreName);

        string? normalizedSellerBio =
            NormalizeOptionalValue(request.SellerBio);

        ValidateOptionalLength(
            normalizedSellerBio,
            MaximumSellerBioLength,
            "SellerBioTooLong");

        await EnsureStoreNameIsUniqueAsync(
            normalizedStoreName,
            user.Id,
            cancellationToken);

        string? previousStoreName = user.StoreName;
        string? previousSellerBio = user.SellerBio;

        user.StoreName = normalizedStoreName;
        user.SellerBio = normalizedSellerBio;

        IdentityResult updateResult =
            await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException(
                FormatIdentityErrors(updateResult.Errors));
        }

        IdentityResult roleResult =
            await _userManager.AddToRoleAsync(
                user,
                ApplicationRoles.Seller);

        if (!roleResult.Succeeded)
        {
            await RollbackSellerProfileAsync(
                user,
                previousStoreName,
                previousSellerBio,
                roleResult.Errors);

            throw new InvalidOperationException(
                FormatIdentityErrors(roleResult.Errors));
        }

        return MapToSellerProfileResponse(
            user,
            isSeller: true);
    }

    public async Task<SellerProfileResponse>
        GetCurrentSellerProfileAsync(
            CancellationToken cancellationToken = default)
    {
        ApplicationUser user =
            await GetCurrentActiveUserAsync(cancellationToken);

        await EnsureUserIsSellerAsync(user);

        return MapToSellerProfileResponse(
            user,
            isSeller: true);
    }

    public async Task<SellerProfileResponse>
        UpdateCurrentSellerProfileAsync(
            UpdateSellerProfileRequest request,
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ApplicationUser user =
            await GetCurrentActiveUserAsync(cancellationToken);

        await EnsureUserIsSellerAsync(user);

        string normalizedStoreName =
            NormalizeAndValidateStoreName(request.StoreName);

        string? normalizedSellerBio =
            NormalizeOptionalValue(request.SellerBio);

        string? normalizedCity =
            NormalizeOptionalValue(request.City);

        string? normalizedAddress =
            NormalizeOptionalValue(request.Address);

        ValidateOptionalLength(
            normalizedSellerBio,
            MaximumSellerBioLength,
            "SellerBioTooLong");

        ValidateOptionalLength(
            normalizedCity,
            MaximumCityLength,
            "CityTooLong");

        ValidateOptionalLength(
            normalizedAddress,
            MaximumAddressLength,
            "AddressTooLong");

        await EnsureStoreNameIsUniqueAsync(
            normalizedStoreName,
            user.Id,
            cancellationToken);

        user.StoreName = normalizedStoreName;
        user.SellerBio = normalizedSellerBio;
        user.City = normalizedCity;
        user.Address = normalizedAddress;

        IdentityResult updateResult =
            await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException(
                FormatIdentityErrors(updateResult.Errors));
        }

        return MapToSellerProfileResponse(
            user,
            isSeller: true);
    }

    public async Task<PublicSellerProfileResponse>
        GetPublicSellerProfileAsync(
            string sellerId,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sellerId))
        {
            throw new KeyNotFoundException(
                "SellerNotFound");
        }

        string normalizedSellerId = sellerId.Trim();

        ApplicationUser? seller = await _userManager
            .Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                user =>
                    user.Id == normalizedSellerId &&
                    !user.IsDeleted,
                cancellationToken);

        if (seller is null)
        {
            throw new KeyNotFoundException(
                "SellerNotFound");
        }

        bool isSeller = await _userManager.IsInRoleAsync(
            seller,
            ApplicationRoles.Seller);

        if (!isSeller ||
            string.IsNullOrWhiteSpace(seller.StoreName))
        {
            throw new KeyNotFoundException(
                "SellerNotFound");
        }

        return new PublicSellerProfileResponse
        {
            UserId = seller.Id,
            FullName = seller.FullName,
            StoreName = seller.StoreName,
            SellerBio = seller.SellerBio,
            SellerRating = seller.SellerRating,
            City = seller.City
        };
    }

    private async Task<ApplicationUser>
        GetCurrentActiveUserAsync(
            CancellationToken cancellationToken)
    {
        string userId = _currentUserService.GetUserId();

        ApplicationUser? user = await _userManager
            .Users
            .FirstOrDefaultAsync(
                applicationUser =>
                    applicationUser.Id == userId &&
                    !applicationUser.IsDeleted,
                cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException(
                "UserNotFound");
        }

        return user;
    }

    private async Task EnsureUserIsSellerAsync(
        ApplicationUser user)
    {
        bool isSeller = await _userManager.IsInRoleAsync(
            user,
            ApplicationRoles.Seller);

        if (!isSeller)
        {
            throw new UnauthorizedAccessException(
                "SellerRoleRequired");
        }
    }

    private async Task EnsureStoreNameIsUniqueAsync(
        string normalizedStoreName,
        string excludedUserId,
        CancellationToken cancellationToken)
    {
        string normalizedUpperStoreName =
            normalizedStoreName.ToUpper();

        bool storeNameExists = await _userManager
            .Users
            .AsNoTracking()
            .AnyAsync(
                user =>
                    !user.IsDeleted &&
                    user.Id != excludedUserId &&
                    user.StoreName != null &&
                    user.StoreName.ToUpper() ==
                    normalizedUpperStoreName,
                cancellationToken);

        if (storeNameExists)
        {
            throw new ConflictException(
                "StoreNameAlreadyExists");
        }
    }

    private async Task RollbackSellerProfileAsync(
        ApplicationUser user,
        string? previousStoreName,
        string? previousSellerBio,
        IEnumerable<IdentityError> roleErrors)
    {
        user.StoreName = previousStoreName;
        user.SellerBio = previousSellerBio;

        IdentityResult rollbackResult =
            await _userManager.UpdateAsync(user);

        if (!rollbackResult.Succeeded)
        {
            List<IdentityError> errors =
                roleErrors.ToList();

            errors.AddRange(rollbackResult.Errors);

            throw new InvalidOperationException(
                FormatIdentityErrors(errors));
        }
    }

    private static SellerProfileResponse
        MapToSellerProfileResponse(
            ApplicationUser user,
            bool isSeller)
    {
        return new SellerProfileResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            StoreName = user.StoreName ?? string.Empty,
            SellerBio = user.SellerBio,
            SellerRating = user.SellerRating,
            City = user.City,
            Address = user.Address,
            IsSeller = isSeller
        };
    }

    private static string NormalizeAndValidateStoreName(
        string? storeName)
    {
        if (string.IsNullOrWhiteSpace(storeName))
        {
            throw new InvalidOperationException(
                "StoreNameRequired");
        }

        string normalizedStoreName = storeName.Trim();

        if (normalizedStoreName.Length >
            MaximumStoreNameLength)
        {
            throw new InvalidOperationException(
                "StoreNameTooLong");
        }

        return normalizedStoreName;
    }

    private static string? NormalizeOptionalValue(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static void ValidateOptionalLength(
        string? value,
        int maximumLength,
        string errorKey)
    {
        if (value is not null &&
            value.Length > maximumLength)
        {
            throw new InvalidOperationException(
                errorKey);
        }
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
