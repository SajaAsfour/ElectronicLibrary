using ElectronicLibrary.BLL.Exceptions;
using ElectronicLibrary.DAL.Constants;
using ElectronicLibrary.DAL.DTOs.Requests.Sellers;
using ElectronicLibrary.DAL.Models.Identity;
using ElectronicLibrary.UnitTests.Helpers;
using Microsoft.AspNetCore.Identity;

namespace ElectronicLibrary.UnitTests.Services.Sellers;

public class SellerServiceTests
{
    [Fact]
    public async Task ActivateSellerAsync_WithValidRequest_ActivatesSeller()
    {
        await using var testContext =
            new SellerServiceTestContext();

        ApplicationUser user =
            await testContext.CreateUserAsync(
                id: testContext
                    .CurrentUserService.UserId,
                userName: "current-customer",
                fullName: "Current Customer",
                email: "customer@example.com");

        var request = new ActivateSellerRequest
        {
            StoreName = "  Readers Corner  ",
            SellerBio = "  Books for everyone.  "
        };

        var response =
            await testContext.SellerService
                .ActivateSellerAsync(request);

        ApplicationUser? savedUser =
            await testContext.UserManager
                .FindByIdAsync(user.Id);

        Assert.NotNull(savedUser);

        Assert.Equal(
            "Readers Corner",
            response.StoreName);

        Assert.Equal(
            "Books for everyone.",
            response.SellerBio);

        Assert.Equal(
            "Current Customer",
            response.FullName);

        Assert.Equal(
            "customer@example.com",
            response.Email);

        Assert.True(response.IsSeller);

        Assert.Equal(
            "Readers Corner",
            savedUser!.StoreName);

        Assert.Equal(
            "Books for everyone.",
            savedUser.SellerBio);

        Assert.True(
            await testContext.UserManager
                .IsInRoleAsync(
                    savedUser,
                    ApplicationRoles.Seller));
    }

    [Fact]
    public async Task ActivateSellerAsync_WithDuplicateStoreName_ThrowsConflictException()
    {
        await using var testContext =
            new SellerServiceTestContext();

        await testContext.CreateUserAsync(
            id: testContext
                .CurrentUserService.UserId,
            userName: "current-customer");

        await testContext.CreateUserAsync(
            id: "existing-seller-id",
            userName: "existing-seller",
            storeName: "Readers Corner",
            isSeller: true);

        var request = new ActivateSellerRequest
        {
            StoreName = "  readers corner  "
        };

        var exception =
            await Assert.ThrowsAsync<
                ConflictException>(
                () =>
                    testContext.SellerService
                        .ActivateSellerAsync(
                            request));

        Assert.Equal(
            "StoreNameAlreadyExists",
            exception.Message);
    }

    [Fact]
    public async Task ActivateSellerAsync_WhenAlreadySeller_ThrowsConflictException()
    {
        await using var testContext =
            new SellerServiceTestContext();

        await testContext.CreateUserAsync(
            id: testContext
                .CurrentUserService.UserId,
            userName: "current-seller",
            storeName: "Current Store",
            isSeller: true);

        var request = new ActivateSellerRequest
        {
            StoreName = "Another Store"
        };

        var exception =
            await Assert.ThrowsAsync<
                ConflictException>(
                () =>
                    testContext.SellerService
                        .ActivateSellerAsync(
                            request));

        Assert.Equal(
            "SellerProfileAlreadyActivated",
            exception.Message);
    }

    [Fact]
    public async Task ActivateSellerAsync_WithBlankStoreName_ThrowsInvalidOperationException()
    {
        await using var testContext =
            new SellerServiceTestContext();

        await testContext.CreateUserAsync(
            id: testContext
                .CurrentUserService.UserId,
            userName: "current-customer");

        var request = new ActivateSellerRequest
        {
            StoreName = "   "
        };

        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    testContext.SellerService
                        .ActivateSellerAsync(
                            request));

        Assert.Equal(
            "StoreNameRequired",
            exception.Message);
    }

    [Fact]
    public async Task ActivateSellerAsync_WithoutCurrentUser_ThrowsUnauthorizedAccessException()
    {
        await using var testContext =
            new SellerServiceTestContext();

        testContext.CurrentUserService.UserId =
            string.Empty;

        var request = new ActivateSellerRequest
        {
            StoreName = "Readers Corner"
        };

        var exception =
            await Assert.ThrowsAsync<
                UnauthorizedAccessException>(
                () =>
                    testContext.SellerService
                        .ActivateSellerAsync(
                            request));

        Assert.Equal(
            "UnauthorizedRequest",
            exception.Message);
    }

    [Fact]
    public async Task GetCurrentSellerProfileAsync_WithSeller_ReturnsProfile()
    {
        await using var testContext =
            new SellerServiceTestContext();

        await testContext.CreateUserAsync(
            id: testContext
                .CurrentUserService.UserId,
            userName: "current-seller",
            fullName: "Current Seller",
            email: "seller@example.com",
            storeName: "Seller Store",
            sellerBio: "Seller biography",
            sellerRating: 4.50m,
            city: "Ramallah",
            address: "Main Street",
            isSeller: true);

        var response =
            await testContext.SellerService
                .GetCurrentSellerProfileAsync();

        Assert.Equal(
            testContext.CurrentUserService.UserId,
            response.UserId);

        Assert.Equal(
            "Current Seller",
            response.FullName);

        Assert.Equal(
            "seller@example.com",
            response.Email);

        Assert.Equal(
            "Seller Store",
            response.StoreName);

        Assert.Equal(
            "Seller biography",
            response.SellerBio);

        Assert.Equal(
            4.50m,
            response.SellerRating);

        Assert.Equal(
            "Ramallah",
            response.City);

        Assert.Equal(
            "Main Street",
            response.Address);

        Assert.True(response.IsSeller);
    }

    [Fact]
    public async Task GetCurrentSellerProfileAsync_WithNonSeller_ThrowsUnauthorizedAccessException()
    {
        await using var testContext =
            new SellerServiceTestContext();

        await testContext.CreateUserAsync(
            id: testContext
                .CurrentUserService.UserId,
            userName: "normal-customer");

        var exception =
            await Assert.ThrowsAsync<
                UnauthorizedAccessException>(
                () =>
                    testContext.SellerService
                        .GetCurrentSellerProfileAsync());

        Assert.Equal(
            "SellerRoleRequired",
            exception.Message);
    }

    [Fact]
    public async Task UpdateCurrentSellerProfileAsync_WithValidRequest_UpdatesProfile()
    {
        await using var testContext =
            new SellerServiceTestContext();

        ApplicationUser user =
            await testContext.CreateUserAsync(
                id: testContext
                    .CurrentUserService.UserId,
                userName: "current-seller",
                storeName: "Old Store",
                sellerBio: "Old biography",
                sellerRating: 4.25m,
                city: "Old City",
                address: "Old Address",
                isSeller: true);

        var request =
            new UpdateSellerProfileRequest
            {
                StoreName = "  Updated Store  ",
                SellerBio =
                    "  Updated biography  ",
                City = "  Ramallah  ",
                Address = "  Main Street  "
            };

        var response =
            await testContext.SellerService
                .UpdateCurrentSellerProfileAsync(
                    request);

        ApplicationUser? savedUser =
            await testContext.UserManager
                .FindByIdAsync(user.Id);

        Assert.NotNull(savedUser);

        Assert.Equal(
            "Updated Store",
            response.StoreName);

        Assert.Equal(
            "Updated biography",
            response.SellerBio);

        Assert.Equal(
            "Ramallah",
            response.City);

        Assert.Equal(
            "Main Street",
            response.Address);

        Assert.Equal(
            4.25m,
            response.SellerRating);

        Assert.Equal(
            "Updated Store",
            savedUser!.StoreName);

        Assert.Equal(
            "Updated biography",
            savedUser.SellerBio);

        Assert.Equal(
            "Ramallah",
            savedUser.City);

        Assert.Equal(
            "Main Street",
            savedUser.Address);

        Assert.Equal(
            4.25m,
            savedUser.SellerRating);
    }

    [Fact]
    public async Task UpdateCurrentSellerProfileAsync_WithSameStoreName_Succeeds()
    {
        await using var testContext =
            new SellerServiceTestContext();

        await testContext.CreateUserAsync(
            id: testContext
                .CurrentUserService.UserId,
            userName: "current-seller",
            storeName: "Current Store",
            isSeller: true);

        var request =
            new UpdateSellerProfileRequest
            {
                StoreName = "  Current Store  ",
                SellerBio = "Updated biography"
            };

        var response =
            await testContext.SellerService
                .UpdateCurrentSellerProfileAsync(
                    request);

        Assert.Equal(
            "Current Store",
            response.StoreName);

        Assert.Equal(
            "Updated biography",
            response.SellerBio);
    }

    [Fact]
    public async Task UpdateCurrentSellerProfileAsync_WithDuplicateStoreName_ThrowsConflictException()
    {
        await using var testContext =
            new SellerServiceTestContext();

        await testContext.CreateUserAsync(
            id: testContext
                .CurrentUserService.UserId,
            userName: "current-seller",
            storeName: "Current Store",
            isSeller: true);

        await testContext.CreateUserAsync(
            id: "other-seller-id",
            userName: "other-seller",
            storeName: "Other Store",
            isSeller: true);

        var request =
            new UpdateSellerProfileRequest
            {
                StoreName = "  other store  "
            };

        var exception =
            await Assert.ThrowsAsync<
                ConflictException>(
                () =>
                    testContext.SellerService
                        .UpdateCurrentSellerProfileAsync(
                            request));

        Assert.Equal(
            "StoreNameAlreadyExists",
            exception.Message);
    }

    [Fact]
    public async Task GetPublicSellerProfileAsync_WithSeller_ReturnsOnlyPublicData()
    {
        await using var testContext =
            new SellerServiceTestContext();

        ApplicationUser seller =
            await testContext.CreateUserAsync(
                id: "public-seller-id",
                userName: "public-seller",
                fullName: "Public Seller",
                email: "private@example.com",
                storeName: "Public Store",
                sellerBio: "Public biography",
                sellerRating: 4.75m,
                city: "Nablus",
                address: "Private Address",
                isSeller: true);

        seller.PhoneNumber = "0599000000";
        seller.RefreshTokenHash =
            "private-refresh-token-hash";

        IdentityResult updateResult =
            await testContext.UserManager
                .UpdateAsync(seller);

        Assert.True(updateResult.Succeeded);

        var response =
            await testContext.SellerService
                .GetPublicSellerProfileAsync(
                    seller.Id);

        Assert.Equal(
            seller.Id,
            response.UserId);

        Assert.Equal(
            "Public Seller",
            response.FullName);

        Assert.Equal(
            "Public Store",
            response.StoreName);

        Assert.Equal(
            "Public biography",
            response.SellerBio);

        Assert.Equal(
            4.75m,
            response.SellerRating);

        Assert.Equal(
            "Nablus",
            response.City);

        string[] propertyNames =
            response.GetType()
                .GetProperties()
                .Select(property =>
                    property.Name)
                .ToArray();

        Assert.DoesNotContain(
            "Email",
            propertyNames);

        Assert.DoesNotContain(
            "Address",
            propertyNames);

        Assert.DoesNotContain(
            "PhoneNumber",
            propertyNames);

        Assert.DoesNotContain(
            "RefreshTokenHash",
            propertyNames);
    }

    [Fact]
    public async Task GetPublicSellerProfileAsync_WithNonSeller_ThrowsKeyNotFoundException()
    {
        await using var testContext =
            new SellerServiceTestContext();

        ApplicationUser user =
            await testContext.CreateUserAsync(
                id: "normal-user-id",
                userName: "normal-user");

        var exception =
            await Assert.ThrowsAsync<
                KeyNotFoundException>(
                () =>
                    testContext.SellerService
                        .GetPublicSellerProfileAsync(
                            user.Id));

        Assert.Equal(
            "SellerNotFound",
            exception.Message);
    }

    [Fact]
    public async Task GetCurrentSellerProfileAsync_WithSoftDeletedUser_ThrowsKeyNotFoundException()
    {
        await using var testContext =
            new SellerServiceTestContext();

        await testContext.CreateUserAsync(
            id: testContext
                .CurrentUserService.UserId,
            userName: "deleted-seller",
            storeName: "Deleted Store",
            isSeller: true,
            isDeleted: true);

        var exception =
            await Assert.ThrowsAsync<
                KeyNotFoundException>(
                () =>
                    testContext.SellerService
                        .GetCurrentSellerProfileAsync());

        Assert.Equal(
            "UserNotFound",
            exception.Message);
    }
}
