using ElectronicLibrary.BLL.Exceptions;
using ElectronicLibrary.DAL.DTOs.Requests.Listings;
using ElectronicLibrary.DAL.DTOs.Responses.Listings;
using ElectronicLibrary.DAL.Enums;
using ElectronicLibrary.DAL.Models.Catalog;
using ElectronicLibrary.DAL.Models.Identity;
using ElectronicLibrary.DAL.Models.Marketplace;
using ElectronicLibrary.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ElectronicLibrary.UnitTests.Services.Marketplace;

public class ListingServiceTests
{
    [Fact]
    public async Task
        CreateListingAsync_WithValidRequest_CreatesDraftListing()
    {
        await using var testContext =
            new ListingServiceTestContext();

        ApplicationUser seller =
            await CreateCurrentSellerAsync(
                testContext);

        Book book =
            await testContext.CreateBookAsync();

        var request = new CreateListingRequest
        {
            BookId = book.BookId,
            Price = 100m,
            Quantity = 5,
            Format = BookFormat.Physical,
            Condition = BookCondition.LikeNew,
            DiscountPercentage = 10m
        };

        var response =
            await testContext.ListingService
                .CreateListingAsync(request);

        Listing savedListing =
            await testContext.DbContext.Listings
                .SingleAsync();

        Assert.True(
            response.ListingId > 0);

        Assert.Equal(
            ListingStatus.Draft,
            response.Status);

        Assert.Equal(
            seller.Id,
            response.SellerId);

        Assert.Equal(
            "Test Book Store",
            response.StoreName);

        Assert.Equal(
            book.BookId,
            response.BookId);

        Assert.Equal(
            "Listing Test Book",
            response.BookTitle);

        Assert.Equal(
            100m,
            response.Price);

        Assert.Equal(
            10m,
            response.DiscountPercentage);

        Assert.Equal(
            90m,
            response.EffectivePrice);

        Assert.False(
            response.IsAvailable);

        Assert.Equal(
            seller.Id,
            savedListing.SellerId);

        Assert.Equal(
            seller.Id,
            savedListing.CreatedById);

        Assert.NotEqual(
            default,
            savedListing.CreatedAt);

        Assert.False(
            savedListing.IsDeleted);
    }

    [Fact]
    public async Task
        CreateListingAsync_WithMissingBook_ThrowsKeyNotFoundException()
    {
        await using var testContext =
            new ListingServiceTestContext();

        await CreateCurrentSellerAsync(
            testContext);

        var request = new CreateListingRequest
        {
            BookId = 99999,
            Price = 100m,
            Quantity = 5,
            Format = BookFormat.Physical,
            Condition = BookCondition.New,
            DiscountPercentage = 0m
        };

        KeyNotFoundException exception =
            await Assert.ThrowsAsync<
                KeyNotFoundException>(
                () =>
                    testContext.ListingService
                        .CreateListingAsync(
                            request));

        Assert.Equal(
            "BookNotFound",
            exception.Message);

        Assert.Empty(
            await testContext.DbContext
                .Listings
                .ToListAsync());
    }

    [Fact]
    public async Task
        CreateListingAsync_WithPhysicalFormatWithoutCondition_ThrowsInvalidOperationException()
    {
        await using var testContext =
            new ListingServiceTestContext();

        await CreateCurrentSellerAsync(
            testContext);

        Book book =
            await testContext.CreateBookAsync();

        var request = new CreateListingRequest
        {
            BookId = book.BookId,
            Price = 50m,
            Quantity = 2,
            Format = BookFormat.Physical,
            Condition = null,
            DiscountPercentage = 0m
        };

        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    testContext.ListingService
                        .CreateListingAsync(
                            request));

        Assert.Equal(
            "PhysicalListingConditionRequired",
            exception.Message);
    }

    [Fact]
    public async Task
        CreateListingAsync_WithDigitalCondition_ThrowsInvalidOperationException()
    {
        await using var testContext =
            new ListingServiceTestContext();

        await CreateCurrentSellerAsync(
            testContext);

        Book book =
            await testContext.CreateBookAsync();

        var request = new CreateListingRequest
        {
            BookId = book.BookId,
            Price = 30m,
            Quantity = 10,
            Format = BookFormat.Digital,
            Condition = BookCondition.New,
            DiscountPercentage = 0m
        };

        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    testContext.ListingService
                        .CreateListingAsync(
                            request));

        Assert.Equal(
            "NonPhysicalListingConditionNotAllowed",
            exception.Message);
    }

    [Fact]
    public async Task
        CreateListingAsync_WithNonSeller_ThrowsForbiddenException()
    {
        await using var testContext =
            new ListingServiceTestContext();

        await testContext.CreateUserAsync(
            id: testContext
                .CurrentUserService.UserId,
            userName: "normal-customer",
            storeName: null,
            isSeller: false);

        Book book =
            await testContext.CreateBookAsync();

        var request = new CreateListingRequest
        {
            BookId = book.BookId,
            Price = 30m,
            Quantity = 3,
            Format = BookFormat.Digital,
            Condition = null,
            DiscountPercentage = 0m
        };

        ForbiddenException exception =
            await Assert.ThrowsAsync<
                ForbiddenException>(
                () =>
                    testContext.ListingService
                        .CreateListingAsync(
                            request));

        Assert.Equal(
            "SellerRoleRequired",
            exception.Message);
    }

    [Fact]
    public async Task
        UpdateListingAsync_WithOwnedListing_UpdatesValuesAndAuditFields()
    {
        await using var testContext =
            new ListingServiceTestContext();

        ApplicationUser seller =
            await CreateCurrentSellerAsync(
                testContext);

        Book book =
            await testContext.CreateBookAsync();

        Listing listing =
            await testContext.CreateListingAsync(
                book,
                seller);

        var request = new UpdateListingRequest
        {
            Price = 80m,
            Quantity = 7,
            Format = BookFormat.Physical,
            Condition = BookCondition.Good,
            DiscountPercentage = 25m
        };

        var response =
            await testContext.ListingService
                .UpdateListingAsync(
                    listing.ListingId,
                    request);

        Listing savedListing =
            await testContext.DbContext
                .Listings
                .SingleAsync();

        Assert.Equal(
            80m,
            response.Price);

        Assert.Equal(
            25m,
            response.DiscountPercentage);

        Assert.Equal(
            60m,
            response.EffectivePrice);

        Assert.Equal(
            7,
            response.Quantity);

        Assert.Equal(
            BookCondition.Good,
            response.Condition);

        Assert.NotNull(
            savedListing.UpdatedAt);

        Assert.Equal(
            seller.Id,
            savedListing.UpdatedById);
    }

    [Fact]
    public async Task
        UpdateListingAsync_WhenActiveQuantityBecomesZero_ChangesStatusToOutOfStock()
    {
        await using var testContext =
            new ListingServiceTestContext();

        ApplicationUser seller =
            await CreateCurrentSellerAsync(
                testContext);

        Book book =
            await testContext.CreateBookAsync();

        Listing listing =
            await testContext.CreateListingAsync(
                book,
                seller,
                status: ListingStatus.Active);

        var request = new UpdateListingRequest
        {
            Price = listing.Price,
            Quantity = 0,
            Format = listing.Format,
            Condition = listing.Condition,
            DiscountPercentage =
                listing.DiscountPercentage
        };

        var response =
            await testContext.ListingService
                .UpdateListingAsync(
                    listing.ListingId,
                    request);

        Assert.Equal(
            ListingStatus.OutOfStock,
            response.Status);

        Assert.False(
            response.IsAvailable);
    }

    [Fact]
    public async Task
        UpdateListingAsync_WithAnotherSellerListing_ThrowsForbiddenException()
    {
        await using var testContext =
            new ListingServiceTestContext();

        await CreateCurrentSellerAsync(
            testContext);

        ApplicationUser otherSeller =
            await testContext.CreateUserAsync(
                id: "other-seller-id",
                userName: "other-seller",
                storeName: "Other Store",
                isSeller: true);

        Book book =
            await testContext.CreateBookAsync();

        Listing listing =
            await testContext.CreateListingAsync(
                book,
                otherSeller);

        var request = new UpdateListingRequest
        {
            Price = 75m,
            Quantity = 4,
            Format = BookFormat.Physical,
            Condition = BookCondition.Good,
            DiscountPercentage = 0m
        };

        ForbiddenException exception =
            await Assert.ThrowsAsync<
                ForbiddenException>(
                () =>
                    testContext.ListingService
                        .UpdateListingAsync(
                            listing.ListingId,
                            request));

        Assert.Equal(
            "ListingOwnershipRequired",
            exception.Message);
    }

    [Fact]
    public async Task
        UpdateListingStatusAsync_FromDraftToActive_WithStock_ActivatesListing()
    {
        await using var testContext =
            new ListingServiceTestContext();

        ApplicationUser seller =
            await CreateCurrentSellerAsync(
                testContext);

        Book book =
            await testContext.CreateBookAsync();

        Listing listing =
            await testContext.CreateListingAsync(
                book,
                seller,
                quantity: 4,
                status: ListingStatus.Draft);

        var request =
            new UpdateListingStatusRequest
            {
                Status = ListingStatus.Active
            };

        var response =
            await testContext.ListingService
                .UpdateListingStatusAsync(
                    listing.ListingId,
                    request);

        Assert.Equal(
            ListingStatus.Active,
            response.Status);

        Assert.True(
            response.IsAvailable);
    }

    [Fact]
    public async Task
        UpdateListingStatusAsync_ToActiveWithoutStock_ThrowsInvalidOperationException()
    {
        await using var testContext =
            new ListingServiceTestContext();

        ApplicationUser seller =
            await CreateCurrentSellerAsync(
                testContext);

        Book book =
            await testContext.CreateBookAsync();

        Listing listing =
            await testContext.CreateListingAsync(
                book,
                seller,
                quantity: 0,
                status: ListingStatus.Draft);

        var request =
            new UpdateListingStatusRequest
            {
                Status = ListingStatus.Active
            };

        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    testContext.ListingService
                        .UpdateListingStatusAsync(
                            listing.ListingId,
                            request));

        Assert.Equal(
            "ActiveListingRequiresStock",
            exception.Message);
    }

    [Fact]
    public async Task
        UpdateListingStatusAsync_ToSuspended_ThrowsForbiddenException()
    {
        await using var testContext =
            new ListingServiceTestContext();

        ApplicationUser seller =
            await CreateCurrentSellerAsync(
                testContext);

        Book book =
            await testContext.CreateBookAsync();

        Listing listing =
            await testContext.CreateListingAsync(
                book,
                seller);

        var request =
            new UpdateListingStatusRequest
            {
                Status =
                    ListingStatus.Suspended
            };

        ForbiddenException exception =
            await Assert.ThrowsAsync<
                ForbiddenException>(
                () =>
                    testContext.ListingService
                        .UpdateListingStatusAsync(
                            listing.ListingId,
                            request));

        Assert.Equal(
            "SellerCannotSuspendListing",
            exception.Message);
    }

    [Fact]
    public async Task
        GetListingByIdAsync_WithActiveListing_ReturnsListing()
    {
        await using var testContext =
            new ListingServiceTestContext();

        ApplicationUser seller =
            await CreateCurrentSellerAsync(
                testContext);

        Book book =
            await testContext.CreateBookAsync();

        Listing listing =
            await testContext.CreateListingAsync(
                book,
                seller,
                price: 120m,
                quantity: 2,
                discountPercentage: 20m,
                status: ListingStatus.Active);

        var response =
            await testContext.ListingService
                .GetListingByIdAsync(
                    listing.ListingId);

        Assert.Equal(
            listing.ListingId,
            response.ListingId);

        Assert.Equal(
            96m,
            response.EffectivePrice);

        Assert.True(
            response.IsAvailable);
    }

    [Theory]
    [InlineData(ListingStatus.Draft, 5)]
    [InlineData(ListingStatus.OutOfStock, 0)]
    [InlineData(ListingStatus.Suspended, 5)]
    public async Task
        GetListingByIdAsync_WithNonPublicListing_ThrowsKeyNotFoundException(
            ListingStatus status,
            int quantity)
    {
        await using var testContext =
            new ListingServiceTestContext();

        ApplicationUser seller =
            await CreateCurrentSellerAsync(
                testContext);

        Book book =
            await testContext.CreateBookAsync();

        Listing listing =
            await testContext.CreateListingAsync(
                book,
                seller,
                quantity: quantity,
                status: status);

        KeyNotFoundException exception =
            await Assert.ThrowsAsync<
                KeyNotFoundException>(
                () =>
                    testContext.ListingService
                        .GetListingByIdAsync(
                            listing.ListingId));

        Assert.Equal(
            "ListingNotFound",
            exception.Message);
    }

    [Fact]
    public async Task
        DeleteListingAsync_WithOwnedListing_SoftDeletesListing()
    {
        await using var testContext =
            new ListingServiceTestContext();

        ApplicationUser seller =
            await CreateCurrentSellerAsync(
                testContext);

        Book book =
            await testContext.CreateBookAsync();

        Listing listing =
            await testContext.CreateListingAsync(
                book,
                seller);

        await testContext.ListingService
            .DeleteListingAsync(
                listing.ListingId);

        Listing deletedListing =
            await testContext.DbContext
                .Listings
                .IgnoreQueryFilters()
                .SingleAsync(
                    currentListing =>
                        currentListing.ListingId ==
                        listing.ListingId);

        Assert.True(
            deletedListing.IsDeleted);

        Assert.NotNull(
            deletedListing.DeletedAt);

        Assert.Equal(
            seller.Id,
            deletedListing.DeletedById);

        Assert.Empty(
            await testContext.DbContext
                .Listings
                .ToListAsync());
    }

    [Fact]
    public async Task
        GetCurrentSellerListingsAsync_WithStatusFilter_ReturnsOnlyMatchingListings()
    {
        await using var testContext =
            new ListingServiceTestContext();

        ApplicationUser seller =
            await CreateCurrentSellerAsync(
                testContext);

        Book book =
            await testContext.CreateBookAsync();

        await testContext.CreateListingAsync(
            book,
            seller,
            status: ListingStatus.Draft);

        await testContext.CreateListingAsync(
            book,
            seller,
            status: ListingStatus.Active);

        await testContext.CreateListingAsync(
            book,
            seller,
            quantity: 0,
            status: ListingStatus.OutOfStock);

        var request =
            new SellerListingFilterRequest
            {
                Status = ListingStatus.Active,
                PageNumber = 1,
                PageSize = 10
            };

        var response =
            await testContext.ListingService
                .GetCurrentSellerListingsAsync(
                    request);

        Assert.Single(
            response.Items);

        Assert.Equal(
            ListingStatus.Active,
            response.Items.Single().Status);

        Assert.Equal(
            1,
            response.TotalCount);

        Assert.Equal(
            1,
            response.TotalPages);
    }

    [Fact]
    public async Task
        GetCurrentSellerListingsAsync_WithPagination_ReturnsRequestedPage()
    {
        await using var testContext =
            new ListingServiceTestContext();

        ApplicationUser seller =
            await CreateCurrentSellerAsync(
                testContext);

        Book book =
            await testContext.CreateBookAsync();

        for (int index = 1;
             index <= 5;
             index++)
        {
            await testContext.CreateListingAsync(
                book,
                seller,
                price: 10m * index,
                createdAt:
                    DateTime.UtcNow.AddMinutes(
                        index));
        }

        var request =
            new SellerListingFilterRequest
            {
                PageNumber = 2,
                PageSize = 2
            };

        var response =
            await testContext.ListingService
                .GetCurrentSellerListingsAsync(
                    request);

        Assert.Equal(
            2,
            response.Items.Count);

        Assert.Equal(
            5,
            response.TotalCount);

        Assert.Equal(
            3,
            response.TotalPages);

        Assert.True(
            response.HasPreviousPage);

        Assert.True(
            response.HasNextPage);
    }

    [Fact]
    public async Task
        GetBookListingsAsync_ReturnsOnlyActiveInStockListingsOrderedByEffectivePrice()
    {
        await using var testContext =
            new ListingServiceTestContext();

        ApplicationUser seller =
            await CreateCurrentSellerAsync(
                testContext);

        Book book =
            await testContext.CreateBookAsync();

        Listing expensiveListing =
            await testContext.CreateListingAsync(
                book,
                seller,
                price: 100m,
                quantity: 5,
                discountPercentage: 10m,
                status: ListingStatus.Active);

        Listing cheapestListing =
            await testContext.CreateListingAsync(
                book,
                seller,
                price: 80m,
                quantity: 3,
                discountPercentage: 25m,
                status: ListingStatus.Active);

        await testContext.CreateListingAsync(
            book,
            seller,
            price: 20m,
            quantity: 5,
            status: ListingStatus.Draft);

        await testContext.CreateListingAsync(
            book,
            seller,
            price: 30m,
            quantity: 0,
            status: ListingStatus.OutOfStock);

        var request =
            new BookListingFilterRequest
            {
                PageNumber = 1,
                PageSize = 10
            };

        var response =
            await testContext.ListingService
                .GetBookListingsAsync(
                    book.BookId,
                    request);

        Assert.Equal(
            2,
            response.TotalCount);

        ListingResponse[] items =
            response.Items.ToArray();

        Assert.Equal(
            cheapestListing.ListingId,
            items[0].ListingId);

        Assert.Equal(
            60m,
            items[0].EffectivePrice);

        Assert.Equal(
            expensiveListing.ListingId,
            items[1].ListingId);

        Assert.Equal(
            90m,
            items[1].EffectivePrice);

        Assert.All(
            items,
            item =>
            {
                Assert.Equal(
                    ListingStatus.Active,
                    item.Status);

                Assert.True(
                    item.Quantity > 0);

                Assert.True(
                    item.IsAvailable);
            });
    }

    private static async Task<ApplicationUser>
        CreateCurrentSellerAsync(
            ListingServiceTestContext testContext)
    {
        return await testContext.CreateUserAsync(
            id: testContext
                .CurrentUserService.UserId,
            userName: "current-seller",
            storeName: "Test Book Store",
            isSeller: true);
    }
}