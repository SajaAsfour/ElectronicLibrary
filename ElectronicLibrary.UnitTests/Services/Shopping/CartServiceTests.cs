using ElectronicLibrary.DAL.DTOs.Requests.Carts;
using ElectronicLibrary.DAL.DTOs.Responses.Carts;
using ElectronicLibrary.DAL.Enums;
using ElectronicLibrary.DAL.Models.Shopping;
using ElectronicLibrary.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ElectronicLibrary.UnitTests.Services.Shopping;

public class CartServiceTests
{
    [Fact]
    public async Task
        GetCurrentUserCartAsync_WhenCartDoesNotExist_CreatesEmptyCart()
    {
        await using var context =
            new CartServiceTestContext();

        await context.CreateUserAsync(
            "unit-test-customer-id",
            "cart-customer");

        CartResponse response =
            await context.CartService
                .GetCurrentUserCartAsync();

        Assert.True(response.CartId > 0);
        Assert.Equal(
            "unit-test-customer-id",
            response.UserId);
        Assert.Empty(response.Items);
        Assert.Equal(0, response.TotalItems);
        Assert.Equal(0m, response.Subtotal);
        Assert.Equal(0m, response.TotalDiscount);
        Assert.Equal(0m, response.FinalTotal);

        List<Cart> carts =
            await context.DbContext.Carts
                .AsNoTracking()
                .ToListAsync();

        Cart createdCart =
            Assert.Single(carts);

        Assert.Equal(
            "unit-test-customer-id",
            createdCart.UserId);
    }

    [Fact]
    public async Task
        GetCurrentUserCartAsync_WhenCartAlreadyExists_ReturnsExistingCartWithoutCreatingDuplicate()
    {
        await using var context =
            new CartServiceTestContext();

        var customer =
            await context.CreateUserAsync(
                "unit-test-customer-id",
                "existing-cart-customer");

        Cart existingCart =
            await context.CreateCartAsync(
                customer);

        CartResponse response =
            await context.CartService
                .GetCurrentUserCartAsync();

        Assert.Equal(
            existingCart.CartId,
            response.CartId);
        Assert.Empty(response.Items);

        int cartsCount =
            await context.DbContext.Carts
                .CountAsync();

        Assert.Equal(1, cartsCount);
    }

    [Fact]
    public async Task
        GetCurrentUserCartAsync_WhenCartContainsActiveItem_ReturnsItemAndCalculatedTotals()
    {
        await using var context =
            new CartServiceTestContext();

        var customer =
            await context.CreateUserAsync(
                "unit-test-customer-id",
                "active-cart-customer");

        var seller =
            await context.CreateUserAsync(
                "cart-test-seller-id",
                "active-cart-seller",
                storeName: "Cart Test Store");

        var book =
            await context.CreateBookAsync(
                title: "Clean Code",
                mainImageUrl:
                    "/uploads/books/clean-code.jpg");

        var listing =
            await context.CreateListingAsync(
                book,
                seller,
                price: 100m,
                quantity: 5,
                discountPercentage: 10m);

        Cart cart =
            await context.CreateCartAsync(
                customer);

        await context.CreateCartItemAsync(
            cart,
            listing,
            quantity: 2);

        CartResponse response =
            await context.CartService
                .GetCurrentUserCartAsync();

        CartItemResponse item =
            Assert.Single(response.Items);

        Assert.Equal(
            listing.ListingId,
            item.ListingId);
        Assert.Equal(
            book.BookId,
            item.BookId);
        Assert.Equal(
            "Clean Code",
            item.BookTitle);
        Assert.Equal(
            "/uploads/books/clean-code.jpg",
            item.MainImageUrl);
        Assert.Equal(
            seller.Id,
            item.SellerId);
        Assert.Equal(
            "Cart Test Store",
            item.StoreName);

        Assert.Equal(2, item.Quantity);
        Assert.Equal(5, item.AvailableQuantity);

        Assert.Equal(100m, item.UnitPrice);
        Assert.Equal(
            10m,
            item.DiscountPercentage);
        Assert.Equal(
            90m,
            item.EffectiveUnitPrice);

        Assert.Equal(
            200m,
            item.LineSubtotal);
        Assert.Equal(
            20m,
            item.LineDiscount);
        Assert.Equal(
            180m,
            item.LineTotal);

        Assert.True(item.IsAvailable);
        Assert.Null(
            item.AvailabilityMessage);

        Assert.Equal(2, response.TotalItems);
        Assert.Equal(
            200m,
            response.Subtotal);
        Assert.Equal(
            20m,
            response.TotalDiscount);
        Assert.Equal(
            180m,
            response.FinalTotal);
    }

    [Fact]
    public async Task
        GetCurrentUserCartAsync_WhenListingIsSoftDeleted_KeepsItemVisibleAndMarksItUnavailable()
    {
        await using var context =
            new CartServiceTestContext();

        var customer =
            await context.CreateUserAsync(
                "unit-test-customer-id",
                "deleted-listing-customer");

        var seller =
            await context.CreateUserAsync(
                "deleted-listing-seller-id",
                "deleted-listing-seller",
                storeName:
                    "Deleted Listing Store");

        var book =
            await context.CreateBookAsync(
                title:
                    "Unavailable Book");

        var listing =
            await context.CreateListingAsync(
                book,
                seller,
                price: 50m,
                quantity: 3,
                discountPercentage: 0m,
                isDeleted: true);

        Cart cart =
            await context.CreateCartAsync(
                customer);

        await context.CreateCartItemAsync(
            cart,
            listing,
            quantity: 1);

        CartResponse response =
            await context.CartService
                .GetCurrentUserCartAsync();

        CartItemResponse item =
            Assert.Single(response.Items);

        Assert.Equal(
            listing.ListingId,
            item.ListingId);
        Assert.False(item.IsAvailable);
        Assert.Equal(
            "ListingNotAvailable",
            item.AvailabilityMessage);

        Assert.Equal(50m, item.LineSubtotal);
        Assert.Equal(0m, item.LineDiscount);
        Assert.Equal(50m, item.LineTotal);
    }

    [Fact]
    public async Task
        GetCurrentUserCartAsync_WhenBookIsSoftDeleted_KeepsItemVisibleAndMarksBookUnavailable()
    {
        await using var context =
            new CartServiceTestContext();

        var customer =
            await context.CreateUserAsync(
                "unit-test-customer-id",
                "deleted-book-customer");

        var seller =
            await context.CreateUserAsync(
                "deleted-book-seller-id",
                "deleted-book-seller",
                storeName:
                    "Deleted Book Store");

        var book =
            await context.CreateBookAsync(
                title: "Deleted Book",
                isDeleted: true);

        var listing =
            await context.CreateListingAsync(
                book,
                seller,
                price: 75m,
                quantity: 4);

        Cart cart =
            await context.CreateCartAsync(
                customer);

        await context.CreateCartItemAsync(
            cart,
            listing,
            quantity: 1);

        CartResponse response =
            await context.CartService
                .GetCurrentUserCartAsync();

        CartItemResponse item =
            Assert.Single(response.Items);

        Assert.False(item.IsAvailable);
        Assert.Equal(
            "ListingBookNotAvailable",
            item.AvailabilityMessage);
    }

    [Fact]
    public async Task
        GetCurrentUserCartAsync_WhenCurrentUserDoesNotExist_ThrowsUserNotFound()
    {
        await using var context =
            new CartServiceTestContext();

        KeyNotFoundException exception =
            await Assert.ThrowsAsync<
                KeyNotFoundException>(
                () =>
                    context.CartService
                        .GetCurrentUserCartAsync());

        Assert.Equal(
            "UserNotFound",
            exception.Message);

        Assert.Empty(
            await context.DbContext.Carts
                .ToListAsync());
    }

    [Fact]
    public async Task AddCartItemAsync_WhenCartDoesNotExist_CreatesCartAndAddsItem()
    {
        await using var context =
            new CartServiceTestContext();

        var customer =
            await context.CreateUserAsync(
                "unit-test-customer-id",
                "add-item-customer");

        var seller =
            await context.CreateUserAsync(
                "add-item-seller-id",
                "add-item-seller",
                storeName: "Add Item Store");

        var book =
            await context.CreateBookAsync(
                title: "Add Item Book");

        var listing =
            await context.CreateListingAsync(
                book,
                seller,
                price: 80m,
                quantity: 10,
                discountPercentage: 25m);

        var request =
            new AddCartItemRequest
            {
                ListingId =
                    listing.ListingId,
                Quantity = 2
            };

        CartResponse response =
            await context.CartService
                .AddCartItemAsync(request);

        CartItemResponse responseItem =
            Assert.Single(response.Items);

        Assert.Equal(
            listing.ListingId,
            responseItem.ListingId);
        Assert.Equal(2, responseItem.Quantity);
        Assert.Equal(80m, responseItem.UnitPrice);
        Assert.Equal(
            60m,
            responseItem.EffectiveUnitPrice);
        Assert.Equal(
            160m,
            responseItem.LineSubtotal);
        Assert.Equal(
            40m,
            responseItem.LineDiscount);
        Assert.Equal(
            120m,
            responseItem.LineTotal);

        Cart storedCart =
            Assert.Single(
                await context.DbContext.Carts
                    .AsNoTracking()
                    .ToListAsync());

        Assert.Equal(
            customer.Id,
            storedCart.UserId);

        CartItem storedItem =
            Assert.Single(
                await context.DbContext.CartItems
                    .AsNoTracking()
                    .ToListAsync());

        Assert.Equal(
            storedCart.CartId,
            storedItem.CartId);
        Assert.Equal(
            listing.ListingId,
            storedItem.ListingId);
        Assert.Equal(2, storedItem.Quantity);
    }

    [Fact]
    public async Task AddCartItemAsync_WhenItemAlreadyExists_IncreasesExistingQuantity()
    {
        await using var context =
            new CartServiceTestContext();

        var customer =
            await context.CreateUserAsync(
                "unit-test-customer-id",
                "increase-item-customer");

        var seller =
            await context.CreateUserAsync(
                "increase-item-seller-id",
                "increase-item-seller",
                storeName:
                    "Increase Item Store");

        var book =
            await context.CreateBookAsync(
                title: "Increase Item Book");

        var listing =
            await context.CreateListingAsync(
                book,
                seller,
                price: 50m,
                quantity: 10);

        Cart cart =
            await context.CreateCartAsync(
                customer);

        await context.CreateCartItemAsync(
            cart,
            listing,
            quantity: 2);

        var request =
            new AddCartItemRequest
            {
                ListingId =
                    listing.ListingId,
                Quantity = 3
            };

        CartResponse response =
            await context.CartService
                .AddCartItemAsync(request);

        CartItemResponse item =
            Assert.Single(response.Items);

        Assert.Equal(5, item.Quantity);
        Assert.Equal(5, response.TotalItems);
        Assert.Equal(250m, response.Subtotal);
        Assert.Equal(250m, response.FinalTotal);

        List<CartItem> storedItems =
            await context.DbContext.CartItems
                .AsNoTracking()
                .ToListAsync();

        CartItem storedItem =
            Assert.Single(storedItems);

        Assert.Equal(5, storedItem.Quantity);
    }

    [Fact]
    public async Task AddCartItemAsync_WhenCombinedQuantityExceedsStock_ThrowsInsufficientStock()
    {
        await using var context =
            new CartServiceTestContext();

        var customer =
            await context.CreateUserAsync(
                "unit-test-customer-id",
                "stock-customer");

        var seller =
            await context.CreateUserAsync(
                "stock-seller-id",
                "stock-seller",
                storeName: "Stock Store");

        var book =
            await context.CreateBookAsync(
                title: "Limited Stock Book");

        var listing =
            await context.CreateListingAsync(
                book,
                seller,
                quantity: 4);

        Cart cart =
            await context.CreateCartAsync(
                customer);

        await context.CreateCartItemAsync(
            cart,
            listing,
            quantity: 3);

        var request =
            new AddCartItemRequest
            {
                ListingId =
                    listing.ListingId,
                Quantity = 2
            };

        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    context.CartService
                        .AddCartItemAsync(
                            request));

        Assert.Equal(
            "InsufficientStock",
            exception.Message);

        CartItem storedItem =
            await context.DbContext.CartItems
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(3, storedItem.Quantity);
    }

    [Fact]
    public async Task AddCartItemAsync_WhenListingDoesNotExist_ThrowsListingNotFound()
    {
        await using var context =
            new CartServiceTestContext();

        await context.CreateUserAsync(
            "unit-test-customer-id",
            "missing-listing-customer");

        var request =
            new AddCartItemRequest
            {
                ListingId = 999999,
                Quantity = 1
            };

        KeyNotFoundException exception =
            await Assert.ThrowsAsync<
                KeyNotFoundException>(
                () =>
                    context.CartService
                        .AddCartItemAsync(
                            request));

        Assert.Equal(
            "ListingNotFound",
            exception.Message);

        Assert.Empty(
            await context.DbContext.Carts
                .AsNoTracking()
                .ToListAsync());

        Assert.Empty(
            await context.DbContext.CartItems
                .AsNoTracking()
                .ToListAsync());
    }

    [Fact]
    public async Task AddCartItemAsync_WhenListingIsNotActive_ThrowsListingNotAvailable()
    {
        await using var context =
            new CartServiceTestContext();

        await context.CreateUserAsync(
            "unit-test-customer-id",
            "inactive-listing-customer");

        var seller =
            await context.CreateUserAsync(
                "inactive-listing-seller-id",
                "inactive-listing-seller",
                storeName:
                    "Inactive Listing Store");

        var book =
            await context.CreateBookAsync(
                title: "Inactive Listing Book");

        var listing =
            await context.CreateListingAsync(
                book,
                seller,
                quantity: 5,
                status: ListingStatus.Suspended);

        var request =
            new AddCartItemRequest
            {
                ListingId =
                    listing.ListingId,
                Quantity = 1
            };

        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    context.CartService
                        .AddCartItemAsync(
                            request));

        Assert.Equal(
            "ListingNotAvailable",
            exception.Message);

        Assert.Empty(
            await context.DbContext.CartItems
                .AsNoTracking()
                .ToListAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task AddCartItemAsync_WhenQuantityIsInvalid_ThrowsValidationError(
        int quantity)
    {
        await using var context =
            new CartServiceTestContext();

        var request =
            new AddCartItemRequest
            {
                ListingId = 1,
                Quantity = quantity
            };

        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    context.CartService
                        .AddCartItemAsync(
                            request));

        Assert.Equal(
            "CartQuantityMustBeGreaterThanZero",
            exception.Message);
    }

    [Fact]
    public async Task UpdateCartItemQuantityAsync_WhenItemExists_UpdatesQuantityAndTotals()
    {
        await using var context =
            new CartServiceTestContext();

        var customer =
            await context.CreateUserAsync(
                "unit-test-customer-id",
                "update-item-customer");

        var seller =
            await context.CreateUserAsync(
                "update-item-seller-id",
                "update-item-seller",
                storeName: "Update Item Store");

        var book =
            await context.CreateBookAsync(
                title: "Update Quantity Book");

        var listing =
            await context.CreateListingAsync(
                book,
                seller,
                price: 120m,
                quantity: 10,
                discountPercentage: 25m);

        Cart cart =
            await context.CreateCartAsync(
                customer);

        await context.CreateCartItemAsync(
            cart,
            listing,
            quantity: 2);

        var request =
            new UpdateCartItemQuantityRequest
            {
                Quantity = 4
            };

        CartResponse response =
            await context.CartService
                .UpdateCartItemQuantityAsync(
                    listing.ListingId,
                    request);

        CartItemResponse item =
            Assert.Single(response.Items);

        Assert.Equal(4, item.Quantity);
        Assert.Equal(10, item.AvailableQuantity);
        Assert.Equal(120m, item.UnitPrice);
        Assert.Equal(90m, item.EffectiveUnitPrice);
        Assert.Equal(480m, item.LineSubtotal);
        Assert.Equal(120m, item.LineDiscount);
        Assert.Equal(360m, item.LineTotal);

        Assert.Equal(4, response.TotalItems);
        Assert.Equal(480m, response.Subtotal);
        Assert.Equal(120m, response.TotalDiscount);
        Assert.Equal(360m, response.FinalTotal);

        CartItem storedItem =
            await context.DbContext.CartItems
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(4, storedItem.Quantity);
    }

    [Fact]
    public async Task UpdateCartItemQuantityAsync_WhenQuantityExceedsStock_ThrowsInsufficientStock()
    {
        await using var context =
            new CartServiceTestContext();

        var customer =
            await context.CreateUserAsync(
                "unit-test-customer-id",
                "update-stock-customer");

        var seller =
            await context.CreateUserAsync(
                "update-stock-seller-id",
                "update-stock-seller",
                storeName: "Update Stock Store");

        var book =
            await context.CreateBookAsync(
                title: "Update Stock Book");

        var listing =
            await context.CreateListingAsync(
                book,
                seller,
                quantity: 5);

        Cart cart =
            await context.CreateCartAsync(
                customer);

        await context.CreateCartItemAsync(
            cart,
            listing,
            quantity: 2);

        var request =
            new UpdateCartItemQuantityRequest
            {
                Quantity = 6
            };

        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    context.CartService
                        .UpdateCartItemQuantityAsync(
                            listing.ListingId,
                            request));

        Assert.Equal(
            "InsufficientStock",
            exception.Message);

        CartItem storedItem =
            await context.DbContext.CartItems
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(2, storedItem.Quantity);
    }

    [Fact]
    public async Task UpdateCartItemQuantityAsync_WhenCartDoesNotExist_ThrowsCartItemNotFound()
    {
        await using var context =
            new CartServiceTestContext();

        await context.CreateUserAsync(
            "unit-test-customer-id",
            "missing-cart-customer");

        var request =
            new UpdateCartItemQuantityRequest
            {
                Quantity = 2
            };

        KeyNotFoundException exception =
            await Assert.ThrowsAsync<
                KeyNotFoundException>(
                () =>
                    context.CartService
                        .UpdateCartItemQuantityAsync(
                            1,
                            request));

        Assert.Equal(
            "CartItemNotFound",
            exception.Message);
    }

    [Fact]
    public async Task UpdateCartItemQuantityAsync_WhenItemDoesNotExist_ThrowsCartItemNotFound()
    {
        await using var context =
            new CartServiceTestContext();

        var customer =
            await context.CreateUserAsync(
                "unit-test-customer-id",
                "missing-item-customer");

        await context.CreateCartAsync(
            customer);

        var request =
            new UpdateCartItemQuantityRequest
            {
                Quantity = 2
            };

        KeyNotFoundException exception =
            await Assert.ThrowsAsync<
                KeyNotFoundException>(
                () =>
                    context.CartService
                        .UpdateCartItemQuantityAsync(
                            999999,
                            request));

        Assert.Equal(
            "CartItemNotFound",
            exception.Message);
    }

    [Fact]
    public async Task UpdateCartItemQuantityAsync_WhenListingIsUnavailable_ThrowsListingNotAvailable()
    {
        await using var context =
            new CartServiceTestContext();

        var customer =
            await context.CreateUserAsync(
                "unit-test-customer-id",
                "unavailable-update-customer");

        var seller =
            await context.CreateUserAsync(
                "unavailable-update-seller-id",
                "unavailable-update-seller",
                storeName:
                    "Unavailable Update Store");

        var book =
            await context.CreateBookAsync(
                title:
                    "Unavailable Update Book");

        var listing =
            await context.CreateListingAsync(
                book,
                seller,
                quantity: 5,
                status: ListingStatus.Suspended);

        Cart cart =
            await context.CreateCartAsync(
                customer);

        await context.CreateCartItemAsync(
            cart,
            listing,
            quantity: 1);

        var request =
            new UpdateCartItemQuantityRequest
            {
                Quantity = 2
            };

        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    context.CartService
                        .UpdateCartItemQuantityAsync(
                            listing.ListingId,
                            request));

        Assert.Equal(
            "ListingNotAvailable",
            exception.Message);

        CartItem storedItem =
            await context.DbContext.CartItems
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(1, storedItem.Quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateCartItemQuantityAsync_WhenQuantityIsInvalid_ThrowsValidationError(
        int quantity)
    {
        await using var context =
            new CartServiceTestContext();

        var request =
            new UpdateCartItemQuantityRequest
            {
                Quantity = quantity
            };

        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    context.CartService
                        .UpdateCartItemQuantityAsync(
                            1,
                            request));

        Assert.Equal(
            "CartQuantityMustBeGreaterThanZero",
            exception.Message);
    }

    [Fact]
    public async Task RemoveCartItemAsync_WhenItemExists_RemovesOnlyRequestedItem()
    {
        await using var context =
            new CartServiceTestContext();

        var customer =
            await context.CreateUserAsync(
                "unit-test-customer-id",
                "remove-item-customer");

        var seller =
            await context.CreateUserAsync(
                "remove-item-seller-id",
                "remove-item-seller",
                storeName: "Remove Item Store");

        var firstBook =
            await context.CreateBookAsync(
                title: "First Remove Book");

        var secondBook =
            await context.CreateBookAsync(
                title: "Second Remove Book");

        var firstListing =
            await context.CreateListingAsync(
                firstBook,
                seller,
                quantity: 10);

        var secondListing =
            await context.CreateListingAsync(
                secondBook,
                seller,
                quantity: 10);

        Cart cart =
            await context.CreateCartAsync(
                customer);

        await context.CreateCartItemAsync(
            cart,
            firstListing,
            quantity: 2);

        await context.CreateCartItemAsync(
            cart,
            secondListing,
            quantity: 3);

        await context.CartService
            .RemoveCartItemAsync(
                firstListing.ListingId);

        List<CartItem> storedItems =
            await context.DbContext.CartItems
                .AsNoTracking()
                .ToListAsync();

        CartItem remainingItem =
            Assert.Single(storedItems);

        Assert.Equal(
            secondListing.ListingId,
            remainingItem.ListingId);
        Assert.Equal(3, remainingItem.Quantity);

        Assert.Single(
            await context.DbContext.Carts
                .AsNoTracking()
                .ToListAsync());
    }

    [Fact]
    public async Task RemoveCartItemAsync_WhenAnotherUserOwnsItem_ThrowsCartItemNotFound()
    {
        await using var context =
            new CartServiceTestContext();

        await context.CreateUserAsync(
            "unit-test-customer-id",
            "current-remove-customer");

        var anotherCustomer =
            await context.CreateUserAsync(
                "another-remove-customer-id",
                "another-remove-customer");

        var seller =
            await context.CreateUserAsync(
                "another-remove-seller-id",
                "another-remove-seller",
                storeName:
                    "Another Remove Store");

        var book =
            await context.CreateBookAsync(
                title:
                    "Another Customer Book");

        var listing =
            await context.CreateListingAsync(
                book,
                seller,
                quantity: 10);

        Cart anotherCart =
            await context.CreateCartAsync(
                anotherCustomer);

        await context.CreateCartItemAsync(
            anotherCart,
            listing,
            quantity: 1);

        KeyNotFoundException exception =
            await Assert.ThrowsAsync<
                KeyNotFoundException>(
                () =>
                    context.CartService
                        .RemoveCartItemAsync(
                            listing.ListingId));

        Assert.Equal(
            "CartItemNotFound",
            exception.Message);

        CartItem storedItem =
            await context.DbContext.CartItems
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            anotherCart.CartId,
            storedItem.CartId);
    }

    [Fact]
    public async Task RemoveCartItemAsync_WhenCartDoesNotExist_ThrowsCartItemNotFound()
    {
        await using var context =
            new CartServiceTestContext();

        await context.CreateUserAsync(
            "unit-test-customer-id",
            "remove-missing-cart-customer");

        KeyNotFoundException exception =
            await Assert.ThrowsAsync<
                KeyNotFoundException>(
                () =>
                    context.CartService
                        .RemoveCartItemAsync(1));

        Assert.Equal(
            "CartItemNotFound",
            exception.Message);
    }

    [Fact]
    public async Task RemoveCartItemAsync_WhenItemDoesNotExist_ThrowsCartItemNotFound()
    {
        await using var context =
            new CartServiceTestContext();

        var customer =
            await context.CreateUserAsync(
                "unit-test-customer-id",
                "remove-missing-item-customer");

        await context.CreateCartAsync(
            customer);

        KeyNotFoundException exception =
            await Assert.ThrowsAsync<
                KeyNotFoundException>(
                () =>
                    context.CartService
                        .RemoveCartItemAsync(
                            999999));

        Assert.Equal(
            "CartItemNotFound",
            exception.Message);
    }

    [Fact]
    public async Task ClearCartAsync_WhenCartContainsItems_RemovesAllItemsButKeepsCart()
    {
        await using var context =
            new CartServiceTestContext();

        var customer =
            await context.CreateUserAsync(
                "unit-test-customer-id",
                "clear-cart-customer");

        var seller =
            await context.CreateUserAsync(
                "clear-cart-seller-id",
                "clear-cart-seller",
                storeName: "Clear Cart Store");

        var firstBook =
            await context.CreateBookAsync(
                title: "First Clear Book");

        var secondBook =
            await context.CreateBookAsync(
                title: "Second Clear Book");

        var firstListing =
            await context.CreateListingAsync(
                firstBook,
                seller,
                quantity: 10);

        var secondListing =
            await context.CreateListingAsync(
                secondBook,
                seller,
                quantity: 10);

        Cart cart =
            await context.CreateCartAsync(
                customer);

        await context.CreateCartItemAsync(
            cart,
            firstListing,
            quantity: 2);

        await context.CreateCartItemAsync(
            cart,
            secondListing,
            quantity: 3);

        await context.CartService
            .ClearCartAsync();

        Assert.Empty(
            await context.DbContext.CartItems
                .AsNoTracking()
                .ToListAsync());

        Cart storedCart =
            await context.DbContext.Carts
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            cart.CartId,
            storedCart.CartId);
        Assert.Equal(
            customer.Id,
            storedCart.UserId);
    }

    [Fact]
    public async Task ClearCartAsync_WhenCartDoesNotExist_CompletesWithoutCreatingCart()
    {
        await using var context =
            new CartServiceTestContext();

        await context.CreateUserAsync(
            "unit-test-customer-id",
            "clear-missing-cart-customer");

        await context.CartService
            .ClearCartAsync();

        Assert.Empty(
            await context.DbContext.Carts
                .AsNoTracking()
                .ToListAsync());

        Assert.Empty(
            await context.DbContext.CartItems
                .AsNoTracking()
                .ToListAsync());
    }

    [Fact]
    public async Task ClearCartAsync_WhenCartIsEmpty_CompletesAndKeepsCart()
    {
        await using var context =
            new CartServiceTestContext();

        var customer =
            await context.CreateUserAsync(
                "unit-test-customer-id",
                "clear-empty-cart-customer");

        Cart cart =
            await context.CreateCartAsync(
                customer);

        await context.CartService
            .ClearCartAsync();

        Assert.Empty(
            await context.DbContext.CartItems
                .AsNoTracking()
                .ToListAsync());

        Cart storedCart =
            await context.DbContext.Carts
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            cart.CartId,
            storedCart.CartId);
    }
}
