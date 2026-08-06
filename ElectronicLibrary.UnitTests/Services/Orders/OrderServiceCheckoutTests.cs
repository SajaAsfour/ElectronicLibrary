using ElectronicLibrary.DAL.DTOs.Requests.Orders;
using ElectronicLibrary.DAL.DTOs.Responses.Orders;
using ElectronicLibrary.DAL.Enums;
using ElectronicLibrary.DAL.Models.Identity;
using ElectronicLibrary.DAL.Models.Marketplace;
using ElectronicLibrary.DAL.Models.Shopping;
using ElectronicLibrary.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ElectronicLibrary.UnitTests.Services.Orders;

public class OrderServiceCheckoutTests
{
    [Fact]
    public async Task
        CheckoutAsync_WithValidCartWithoutCoupon_CreatesOrderAndClearsCart()
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser customer =
            await context.CreateUserAsync(
                id: "unit-test-customer-id",
                userName: "checkout-customer");

        ApplicationUser seller =
            await context.CreateUserAsync(
                id: "checkout-seller-id",
                userName: "checkout-seller",
                storeName: "Checkout Store",
                isSeller: true);

        var book =
            await context.CreateBookAsync(
                title: "Checkout Book");

        Listing listing =
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

        var request =
            new CheckoutRequest();

        OrderDetailsResponse response =
            await context.OrderService
                .CheckoutAsync(request);

        Assert.True(response.OrderId > 0);

        Assert.Equal(
            customer.Id,
            response.UserId);

        Assert.Equal(
            OrderStatus.Pending,
            response.Status);

        Assert.Equal(
            2,
            response.TotalItems);

        Assert.Equal(
            200m,
            response.SubtotalAmount);

        Assert.Equal(
            20m,
            response.ListingDiscountAmount);

        Assert.Equal(
            0m,
            response.CouponDiscountAmount);

        Assert.Equal(
            20m,
            response.TotalDiscountAmount);

        Assert.Equal(
            180m,
            response.TotalAmount);

        Assert.Null(response.CouponCode);
        Assert.Null(
            response.CouponDiscountType);
        Assert.Null(
            response.CouponDiscountValue);

        OrderItemResponse responseItem =
            Assert.Single(response.Items);

        Assert.Equal(
            listing.ListingId,
            responseItem.ListingId);

        Assert.Equal(
            book.BookId,
            responseItem.BookId);

        Assert.Equal(
            seller.Id,
            responseItem.SellerId);

        Assert.Equal(
            "Checkout Book",
            responseItem.BookTitle);

        Assert.Equal(
            "Checkout Store",
            responseItem.SellerStoreName);

        Assert.Equal(
            BookFormat.Physical,
            responseItem.Format);

        Assert.Equal(
            BookCondition.New,
            responseItem.Condition);

        Assert.Equal(
            2,
            responseItem.Quantity);

        Assert.Equal(
            100m,
            responseItem.UnitPrice);

        Assert.Equal(
            10m,
            responseItem.DiscountPercentage);

        Assert.Equal(
            90m,
            responseItem.EffectiveUnitPrice);

        Assert.Equal(
            200m,
            responseItem.LineSubtotal);

        Assert.Equal(
            20m,
            responseItem.LineDiscount);

        Assert.Equal(
            180m,
            responseItem.LineTotal);

        Assert.Equal(
            OrderItemStatus.Pending,
            responseItem.Status);

        Listing storedListing =
            await context.DbContext.Listings
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            3,
            storedListing.Quantity);

        Assert.Equal(
            ListingStatus.Active,
            storedListing.Status);

        Assert.Empty(
            await context.DbContext.CartItems
                .AsNoTracking()
                .ToListAsync());

        Assert.Single(
            await context.DbContext.Orders
                .AsNoTracking()
                .ToListAsync());

        Assert.Single(
            await context.DbContext.OrderItems
                .AsNoTracking()
                .ToListAsync());
    }

    [Fact]
    public async Task
        CheckoutAsync_WithPercentageCoupon_AppliesCouponAfterListingDiscount()
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser customer =
            await context.CreateUserAsync(
                id: "unit-test-customer-id",
                userName:
                    "coupon-checkout-customer");

        ApplicationUser seller =
            await context.CreateUserAsync(
                id: "coupon-checkout-seller-id",
                userName:
                    "coupon-checkout-seller",
                storeName: "Coupon Store",
                isSeller: true);

        var book =
            await context.CreateBookAsync(
                title: "Coupon Book");

        Listing listing =
            await context.CreateListingAsync(
                book,
                seller,
                price: 200m,
                quantity: 10,
                discountPercentage: 10m);

        Cart cart =
            await context.CreateCartAsync(
                customer);

        await context.CreateCartItemAsync(
            cart,
            listing,
            quantity: 2);

        var coupon =
            await context.CreateCouponAsync(
                code: "SAVE25",
                discountValue: 25m,
                discountType: "Percentage");

        var request =
            new CheckoutRequest
            {
                CouponCode = "SAVE25"
            };

        OrderDetailsResponse response =
            await context.OrderService
                .CheckoutAsync(request);

        Assert.Equal(
            coupon.CouponId,
            await context.DbContext.Orders
                .AsNoTracking()
                .Where(order =>
                    order.OrderId ==
                    response.OrderId)
                .Select(order =>
                    order.CouponId)
                .SingleAsync());

        Assert.Equal(
            400m,
            response.SubtotalAmount);

        Assert.Equal(
            40m,
            response.ListingDiscountAmount);

        Assert.Equal(
            90m,
            response.CouponDiscountAmount);

        Assert.Equal(
            130m,
            response.TotalDiscountAmount);

        Assert.Equal(
            270m,
            response.TotalAmount);

        Assert.Equal(
            "SAVE25",
            response.CouponCode);

        Assert.Equal(
            "Percentage",
            response.CouponDiscountType);

        Assert.Equal(
            25m,
            response.CouponDiscountValue);

        Assert.Empty(
            await context.DbContext.CartItems
                .AsNoTracking()
                .ToListAsync());

        Listing storedListing =
            await context.DbContext.Listings
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            8,
            storedListing.Quantity);
    }

    [Fact]
    public async Task
        CheckoutAsync_WhenCartIsEmpty_ThrowsCartIsEmptyWithoutCreatingOrder()
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser customer =
            await context.CreateUserAsync(
                id: "unit-test-customer-id",
                userName:
                    "empty-cart-customer");

        await context.CreateCartAsync(
            customer);

        var request =
            new CheckoutRequest();

        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    context.OrderService
                        .CheckoutAsync(request));

        Assert.Equal(
            "CartIsEmpty",
            exception.Message);

        Assert.Empty(
            await context.DbContext.Orders
                .AsNoTracking()
                .ToListAsync());

        Assert.Empty(
            await context.DbContext.OrderItems
                .AsNoTracking()
                .ToListAsync());

        Assert.Single(
            await context.DbContext.Carts
                .AsNoTracking()
                .ToListAsync());
    }
    [Fact]
    public async Task
    CheckoutAsync_WhenCartDoesNotExist_ThrowsCartNotFound()
    {
        await using var context =
            new OrderServiceTestContext();

        await context.CreateUserAsync(
            id: "unit-test-customer-id",
            userName: "missing-cart-customer");

        var request =
            new CheckoutRequest();

        KeyNotFoundException exception =
            await Assert.ThrowsAsync<
                KeyNotFoundException>(
                () =>
                    context.OrderService
                        .CheckoutAsync(request));

        Assert.Equal(
            "CartNotFound",
            exception.Message);

        Assert.Empty(
            await context.DbContext.Orders
                .AsNoTracking()
                .ToListAsync());

        Assert.Empty(
            await context.DbContext.OrderItems
                .AsNoTracking()
                .ToListAsync());
    }

    [Fact]
    public async Task
        CheckoutAsync_WhenRequestedQuantityExceedsStock_ThrowsInsufficientStockWithoutChangingData()
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser customer =
            await context.CreateUserAsync(
                id: "unit-test-customer-id",
                userName:
                    "insufficient-stock-customer");

        ApplicationUser seller =
            await context.CreateUserAsync(
                id: "insufficient-stock-seller-id",
                userName:
                    "insufficient-stock-seller",
                storeName:
                    "Insufficient Stock Store",
                isSeller: true);

        var book =
            await context.CreateBookAsync(
                title: "Limited Order Book");

        Listing listing =
            await context.CreateListingAsync(
                book,
                seller,
                price: 50m,
                quantity: 2);

        Cart cart =
            await context.CreateCartAsync(
                customer);

        await context.CreateCartItemAsync(
            cart,
            listing,
            quantity: 3);

        var request =
            new CheckoutRequest();

        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    context.OrderService
                        .CheckoutAsync(request));

        Assert.Equal(
            "InsufficientStock",
            exception.Message);

        Listing storedListing =
            await context.DbContext.Listings
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            2,
            storedListing.Quantity);

        Assert.Equal(
            ListingStatus.Active,
            storedListing.Status);

        CartItem storedCartItem =
            await context.DbContext.CartItems
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            3,
            storedCartItem.Quantity);

        Assert.Empty(
            await context.DbContext.Orders
                .AsNoTracking()
                .ToListAsync());

        Assert.Empty(
            await context.DbContext.OrderItems
                .AsNoTracking()
                .ToListAsync());
    }

    [Fact]
    public async Task
        CheckoutAsync_WhenPurchasedQuantityUsesAllStock_MarksListingOutOfStock()
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser customer =
            await context.CreateUserAsync(
                id: "unit-test-customer-id",
                userName:
                    "all-stock-customer");

        ApplicationUser seller =
            await context.CreateUserAsync(
                id: "all-stock-seller-id",
                userName:
                    "all-stock-seller",
                storeName: "All Stock Store",
                isSeller: true);

        var book =
            await context.CreateBookAsync(
                title: "Last Available Book");

        Listing listing =
            await context.CreateListingAsync(
                book,
                seller,
                price: 75m,
                quantity: 2);

        Cart cart =
            await context.CreateCartAsync(
                customer);

        await context.CreateCartItemAsync(
            cart,
            listing,
            quantity: 2);

        OrderDetailsResponse response =
            await context.OrderService
                .CheckoutAsync(
                    new CheckoutRequest());

        Assert.Equal(
            150m,
            response.TotalAmount);

        Listing storedListing =
            await context.DbContext.Listings
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            0,
            storedListing.Quantity);

        Assert.Equal(
            ListingStatus.OutOfStock,
            storedListing.Status);

        Assert.Empty(
            await context.DbContext.CartItems
                .AsNoTracking()
                .ToListAsync());

        Assert.Single(
            await context.DbContext.Orders
                .AsNoTracking()
                .ToListAsync());

        Assert.Single(
            await context.DbContext.OrderItems
                .AsNoTracking()
                .ToListAsync());
    }
    [Fact]
    public async Task
    CheckoutAsync_WithFixedCoupon_DeductsFixedAmountAfterListingDiscount()
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser customer =
            await context.CreateUserAsync(
                id: "unit-test-customer-id",
                userName: "fixed-coupon-customer");

        ApplicationUser seller =
            await context.CreateUserAsync(
                id: "fixed-coupon-seller-id",
                userName: "fixed-coupon-seller",
                storeName: "Fixed Coupon Store",
                isSeller: true);

        var book =
            await context.CreateBookAsync(
                title: "Fixed Coupon Book");

        Listing listing =
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

        await context.CreateCouponAsync(
            code: "SAVE50",
            discountValue: 50m,
            discountType: "Fixed");

        var request =
            new CheckoutRequest
            {
                CouponCode = "SAVE50"
            };

        OrderDetailsResponse response =
            await context.OrderService
                .CheckoutAsync(request);

        Assert.Equal(
            200m,
            response.SubtotalAmount);

        Assert.Equal(
            20m,
            response.ListingDiscountAmount);

        Assert.Equal(
            50m,
            response.CouponDiscountAmount);

        Assert.Equal(
            70m,
            response.TotalDiscountAmount);

        Assert.Equal(
            130m,
            response.TotalAmount);

        Assert.Equal(
            "SAVE50",
            response.CouponCode);

        Assert.Equal(
            "Fixed",
            response.CouponDiscountType);

        Assert.Equal(
            50m,
            response.CouponDiscountValue);

        Listing storedListing =
            await context.DbContext.Listings
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            3,
            storedListing.Quantity);

        Assert.Empty(
            await context.DbContext.CartItems
                .AsNoTracking()
                .ToListAsync());
    }

    [Fact]
    public async Task
        CheckoutAsync_WithMissingCoupon_ThrowsCouponNotFoundWithoutChangingStock()
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser customer =
            await context.CreateUserAsync(
                id: "unit-test-customer-id",
                userName: "missing-coupon-customer");

        ApplicationUser seller =
            await context.CreateUserAsync(
                id: "missing-coupon-seller-id",
                userName: "missing-coupon-seller",
                storeName: "Missing Coupon Store",
                isSeller: true);

        var book =
            await context.CreateBookAsync(
                title: "Missing Coupon Book");

        Listing listing =
            await context.CreateListingAsync(
                book,
                seller,
                price: 80m,
                quantity: 4);

        Cart cart =
            await context.CreateCartAsync(
                customer);

        await context.CreateCartItemAsync(
            cart,
            listing,
            quantity: 2);

        var request =
            new CheckoutRequest
            {
                CouponCode = "DOES-NOT-EXIST"
            };

        KeyNotFoundException exception =
            await Assert.ThrowsAsync<
                KeyNotFoundException>(
                () =>
                    context.OrderService
                        .CheckoutAsync(request));

        Assert.Equal(
            "CouponNotFound",
            exception.Message);

        // Remove unsaved tracked changes made before
        // coupon validation in the InMemory provider.
        context.ClearTracking();

        Listing storedListing =
            await context.DbContext.Listings
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            4,
            storedListing.Quantity);

        CartItem storedCartItem =
            await context.DbContext.CartItems
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            2,
            storedCartItem.Quantity);

        Assert.Empty(
            await context.DbContext.Orders
                .AsNoTracking()
                .ToListAsync());

        Assert.Empty(
            await context.DbContext.OrderItems
                .AsNoTracking()
                .ToListAsync());
    }

    [Fact]
    public async Task
        CheckoutAsync_WithInactiveCoupon_ThrowsCouponInactiveWithoutCreatingOrder()
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser customer =
            await context.CreateUserAsync(
                id: "unit-test-customer-id",
                userName: "inactive-coupon-customer");

        ApplicationUser seller =
            await context.CreateUserAsync(
                id: "inactive-coupon-seller-id",
                userName: "inactive-coupon-seller",
                storeName: "Inactive Coupon Store",
                isSeller: true);

        var book =
            await context.CreateBookAsync(
                title: "Inactive Coupon Book");

        Listing listing =
            await context.CreateListingAsync(
                book,
                seller,
                price: 60m,
                quantity: 3);

        Cart cart =
            await context.CreateCartAsync(
                customer);

        await context.CreateCartItemAsync(
            cart,
            listing,
            quantity: 1);

        await context.CreateCouponAsync(
            code: "INACTIVE10",
            discountValue: 10m,
            discountType: "Percentage",
            isActive: false);

        var request =
            new CheckoutRequest
            {
                CouponCode = "INACTIVE10"
            };

        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    context.OrderService
                        .CheckoutAsync(request));

        Assert.Equal(
            "CouponInactive",
            exception.Message);

        context.ClearTracking();

        Listing storedListing =
            await context.DbContext.Listings
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            3,
            storedListing.Quantity);

        Assert.Single(
            await context.DbContext.CartItems
                .AsNoTracking()
                .ToListAsync());

        Assert.Empty(
            await context.DbContext.Orders
                .AsNoTracking()
                .ToListAsync());

        Assert.Empty(
            await context.DbContext.OrderItems
                .AsNoTracking()
                .ToListAsync());
    }
    [Fact]
    public async Task
    CheckoutAsync_WithCouponThatHasNotStarted_ThrowsCouponNotStarted()
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser customer =
            await context.CreateUserAsync(
                id: "unit-test-customer-id",
                userName: "future-coupon-customer");

        ApplicationUser seller =
            await context.CreateUserAsync(
                id: "future-coupon-seller-id",
                userName: "future-coupon-seller",
                storeName: "Future Coupon Store",
                isSeller: true);

        var book =
            await context.CreateBookAsync(
                title: "Future Coupon Book");

        Listing listing =
            await context.CreateListingAsync(
                book,
                seller,
                price: 100m,
                quantity: 5);

        Cart cart =
            await context.CreateCartAsync(
                customer);

        await context.CreateCartItemAsync(
            cart,
            listing,
            quantity: 1);

        await context.CreateCouponAsync(
            code: "FUTURE10",
            discountValue: 10m,
            discountType: "Percentage",
            startDate: DateTime.UtcNow.AddDays(1),
            endDate: DateTime.UtcNow.AddDays(5));

        var request =
            new CheckoutRequest
            {
                CouponCode = "FUTURE10"
            };

        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    context.OrderService
                        .CheckoutAsync(request));

        Assert.Equal(
            "CouponNotStarted",
            exception.Message);

        context.ClearTracking();

        Listing storedListing =
            await context.DbContext.Listings
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            5,
            storedListing.Quantity);

        Assert.Single(
            await context.DbContext.CartItems
                .AsNoTracking()
                .ToListAsync());

        Assert.Empty(
            await context.DbContext.Orders
                .AsNoTracking()
                .ToListAsync());
    }

    [Fact]
    public async Task
        CheckoutAsync_WithExpiredCoupon_ThrowsCouponExpired()
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser customer =
            await context.CreateUserAsync(
                id: "unit-test-customer-id",
                userName: "expired-coupon-customer");

        ApplicationUser seller =
            await context.CreateUserAsync(
                id: "expired-coupon-seller-id",
                userName: "expired-coupon-seller",
                storeName: "Expired Coupon Store",
                isSeller: true);

        var book =
            await context.CreateBookAsync(
                title: "Expired Coupon Book");

        Listing listing =
            await context.CreateListingAsync(
                book,
                seller,
                price: 90m,
                quantity: 4);

        Cart cart =
            await context.CreateCartAsync(
                customer);

        await context.CreateCartItemAsync(
            cart,
            listing,
            quantity: 2);

        await context.CreateCouponAsync(
            code: "EXPIRED20",
            discountValue: 20m,
            discountType: "Percentage",
            startDate: DateTime.UtcNow.AddDays(-5),
            endDate: DateTime.UtcNow.AddDays(-1));

        var request =
            new CheckoutRequest
            {
                CouponCode = "EXPIRED20"
            };

        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    context.OrderService
                        .CheckoutAsync(request));

        Assert.Equal(
            "CouponExpired",
            exception.Message);

        context.ClearTracking();

        Listing storedListing =
            await context.DbContext.Listings
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            4,
            storedListing.Quantity);

        Assert.Single(
            await context.DbContext.CartItems
                .AsNoTracking()
                .ToListAsync());

        Assert.Empty(
            await context.DbContext.Orders
                .AsNoTracking()
                .ToListAsync());
    }

    [Fact]
    public async Task
        CheckoutAsync_WithUnsupportedCouponDiscountType_ThrowsUnsupportedCouponDiscountType()
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser customer =
            await context.CreateUserAsync(
                id: "unit-test-customer-id",
                userName:
                    "unsupported-coupon-customer");

        ApplicationUser seller =
            await context.CreateUserAsync(
                id: "unsupported-coupon-seller-id",
                userName:
                    "unsupported-coupon-seller",
                storeName:
                    "Unsupported Coupon Store",
                isSeller: true);

        var book =
            await context.CreateBookAsync(
                title:
                    "Unsupported Coupon Book");

        Listing listing =
            await context.CreateListingAsync(
                book,
                seller,
                price: 120m,
                quantity: 3);

        Cart cart =
            await context.CreateCartAsync(
                customer);

        await context.CreateCartItemAsync(
            cart,
            listing,
            quantity: 1);

        await context.CreateCouponAsync(
            code: "INVALIDTYPE",
            discountValue: 15m,
            discountType: "BuyOneGetOne");

        var request =
            new CheckoutRequest
            {
                CouponCode = "INVALIDTYPE"
            };

        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    context.OrderService
                        .CheckoutAsync(request));

        Assert.Equal(
            "UnsupportedCouponDiscountType",
            exception.Message);

        context.ClearTracking();

        Listing storedListing =
            await context.DbContext.Listings
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            3,
            storedListing.Quantity);

        Assert.Single(
            await context.DbContext.CartItems
                .AsNoTracking()
                .ToListAsync());

        Assert.Empty(
            await context.DbContext.Orders
                .AsNoTracking()
                .ToListAsync());
    }
    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(101)]
    public async Task
    CheckoutAsync_WithInvalidPercentageCouponValue_ThrowsInvalidCouponDiscount(
        decimal discountValue)
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser customer =
            await context.CreateUserAsync(
                id: "unit-test-customer-id",
                userName:
                    $"invalid-percentage-customer-{discountValue}");

        ApplicationUser seller =
            await context.CreateUserAsync(
                id:
                    $"invalid-percentage-seller-{discountValue}",
                userName:
                    $"invalid-percentage-seller-{discountValue}",
                storeName:
                    "Invalid Percentage Store",
                isSeller: true);

        var book =
            await context.CreateBookAsync(
                title:
                    "Invalid Percentage Coupon Book");

        Listing listing =
            await context.CreateListingAsync(
                book,
                seller,
                price: 100m,
                quantity: 5);

        Cart cart =
            await context.CreateCartAsync(
                customer);

        await context.CreateCartItemAsync(
            cart,
            listing,
            quantity: 2);

        await context.CreateCouponAsync(
            code: "INVALIDPERCENTAGE",
            discountValue: discountValue,
            discountType: "Percentage");

        var request =
            new CheckoutRequest
            {
                CouponCode =
                    "INVALIDPERCENTAGE"
            };

        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    context.OrderService
                        .CheckoutAsync(request));

        Assert.Equal(
            "InvalidCouponDiscount",
            exception.Message);

        context.ClearTracking();

        Listing storedListing =
            await context.DbContext.Listings
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            5,
            storedListing.Quantity);

        CartItem storedCartItem =
            await context.DbContext.CartItems
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            2,
            storedCartItem.Quantity);

        Assert.Empty(
            await context.DbContext.Orders
                .AsNoTracking()
                .ToListAsync());

        Assert.Empty(
            await context.DbContext.OrderItems
                .AsNoTracking()
                .ToListAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-25)]
    public async Task
        CheckoutAsync_WithInvalidFixedCouponValue_ThrowsInvalidCouponDiscount(
            decimal discountValue)
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser customer =
            await context.CreateUserAsync(
                id: "unit-test-customer-id",
                userName:
                    $"invalid-fixed-customer-{discountValue}");

        ApplicationUser seller =
            await context.CreateUserAsync(
                id:
                    $"invalid-fixed-seller-{discountValue}",
                userName:
                    $"invalid-fixed-seller-{discountValue}",
                storeName:
                    "Invalid Fixed Store",
                isSeller: true);

        var book =
            await context.CreateBookAsync(
                title:
                    "Invalid Fixed Coupon Book");

        Listing listing =
            await context.CreateListingAsync(
                book,
                seller,
                price: 80m,
                quantity: 4);

        Cart cart =
            await context.CreateCartAsync(
                customer);

        await context.CreateCartItemAsync(
            cart,
            listing,
            quantity: 1);

        await context.CreateCouponAsync(
            code: "INVALIDFIXED",
            discountValue: discountValue,
            discountType: "Fixed");

        var request =
            new CheckoutRequest
            {
                CouponCode = "INVALIDFIXED"
            };

        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    context.OrderService
                        .CheckoutAsync(request));

        Assert.Equal(
            "InvalidCouponDiscount",
            exception.Message);

        context.ClearTracking();

        Listing storedListing =
            await context.DbContext.Listings
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            4,
            storedListing.Quantity);

        Assert.Single(
            await context.DbContext.CartItems
                .AsNoTracking()
                .ToListAsync());

        Assert.Empty(
            await context.DbContext.Orders
                .AsNoTracking()
                .ToListAsync());
    }

    [Fact]
    public async Task
        CheckoutAsync_WithFixedCouponGreaterThanOrderTotal_CapsDiscountAtOrderTotal()
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser customer =
            await context.CreateUserAsync(
                id: "unit-test-customer-id",
                userName:
                    "large-fixed-coupon-customer");

        ApplicationUser seller =
            await context.CreateUserAsync(
                id: "large-fixed-coupon-seller-id",
                userName:
                    "large-fixed-coupon-seller",
                storeName:
                    "Large Fixed Coupon Store",
                isSeller: true);

        var book =
            await context.CreateBookAsync(
                title:
                    "Large Fixed Coupon Book");

        Listing listing =
            await context.CreateListingAsync(
                book,
                seller,
                price: 100m,
                quantity: 3,
                discountPercentage: 10m);

        Cart cart =
            await context.CreateCartAsync(
                customer);

        await context.CreateCartItemAsync(
            cart,
            listing,
            quantity: 1);

        await context.CreateCouponAsync(
            code: "SAVE500",
            discountValue: 500m,
            discountType: "Fixed");

        OrderDetailsResponse response =
            await context.OrderService
                .CheckoutAsync(
                    new CheckoutRequest
                    {
                        CouponCode = "SAVE500"
                    });

        Assert.Equal(
            100m,
            response.SubtotalAmount);

        Assert.Equal(
            10m,
            response.ListingDiscountAmount);

        Assert.Equal(
            90m,
            response.CouponDiscountAmount);

        Assert.Equal(
            100m,
            response.TotalDiscountAmount);

        Assert.Equal(
            0m,
            response.TotalAmount);

        Assert.Equal(
            "SAVE500",
            response.CouponCode);

        Assert.Equal(
            "Fixed",
            response.CouponDiscountType);

        Assert.Equal(
            500m,
            response.CouponDiscountValue);

        Listing storedListing =
            await context.DbContext.Listings
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            2,
            storedListing.Quantity);

        Assert.Empty(
            await context.DbContext.CartItems
                .AsNoTracking()
                .ToListAsync());
    }
    [Fact]
    public async Task
    CheckoutAsync_WhenCartItemQuantityIsInvalid_ThrowsInvalidCartItemQuantity()
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser customer =
            await context.CreateUserAsync(
                id: "unit-test-customer-id",
                userName:
                    "invalid-cart-quantity-customer");

        ApplicationUser seller =
            await context.CreateUserAsync(
                id: "invalid-cart-quantity-seller-id",
                userName:
                    "invalid-cart-quantity-seller",
                storeName:
                    "Invalid Quantity Store",
                isSeller: true);

        var book =
            await context.CreateBookAsync(
                title:
                    "Invalid Cart Quantity Book");

        Listing listing =
            await context.CreateListingAsync(
                book,
                seller,
                price: 50m,
                quantity: 5);

        Cart cart =
            await context.CreateCartAsync(
                customer);

        await context.CreateCartItemAsync(
            cart,
            listing,
            quantity: 0);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    context.OrderService
                        .CheckoutAsync(
                            new CheckoutRequest()));

        Assert.Equal(
            "InvalidCartItemQuantity",
            exception.Message);

        Assert.Empty(
            await context.DbContext.Orders
                .AsNoTracking()
                .ToListAsync());

        Assert.Empty(
            await context.DbContext.OrderItems
                .AsNoTracking()
                .ToListAsync());

        Assert.Single(
            await context.DbContext.CartItems
                .AsNoTracking()
                .ToListAsync());
    }

    [Fact]
    public async Task
        CheckoutAsync_WhenListingIsNotActive_ThrowsListingNotAvailable()
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser customer =
            await context.CreateUserAsync(
                id: "unit-test-customer-id",
                userName:
                    "inactive-listing-checkout-customer");

        ApplicationUser seller =
            await context.CreateUserAsync(
                id: "inactive-checkout-seller-id",
                userName:
                    "inactive-checkout-seller",
                storeName:
                    "Inactive Checkout Store",
                isSeller: true);

        var book =
            await context.CreateBookAsync(
                title:
                    "Inactive Checkout Book");

        Listing listing =
            await context.CreateListingAsync(
                book,
                seller,
                price: 70m,
                quantity: 5,
                status:
                    ListingStatus.Suspended);

        Cart cart =
            await context.CreateCartAsync(
                customer);

        await context.CreateCartItemAsync(
            cart,
            listing,
            quantity: 1);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    context.OrderService
                        .CheckoutAsync(
                            new CheckoutRequest()));

        Assert.Equal(
            "ListingNotAvailable",
            exception.Message);

        context.ClearTracking();

        Listing storedListing =
            await context.DbContext.Listings
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            5,
            storedListing.Quantity);

        Assert.Equal(
            ListingStatus.Suspended,
            storedListing.Status);

        Assert.Empty(
            await context.DbContext.Orders
                .AsNoTracking()
                .ToListAsync());
    }

    [Fact]
    public async Task
        CheckoutAsync_WhenListingBookIsDeleted_ThrowsListingBookNotAvailable()
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser customer =
            await context.CreateUserAsync(
                id: "unit-test-customer-id",
                userName:
                    "deleted-book-checkout-customer");

        ApplicationUser seller =
            await context.CreateUserAsync(
                id: "deleted-book-checkout-seller-id",
                userName:
                    "deleted-book-checkout-seller",
                storeName:
                    "Deleted Book Checkout Store",
                isSeller: true);

        var book =
            await context.CreateBookAsync(
                title:
                    "Deleted Checkout Book",
                isDeleted: true);

        Listing listing =
            await context.CreateListingAsync(
                book,
                seller,
                price: 90m,
                quantity: 4);

        Cart cart =
            await context.CreateCartAsync(
                customer);

        await context.CreateCartItemAsync(
            cart,
            listing,
            quantity: 1);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    context.OrderService
                        .CheckoutAsync(
                            new CheckoutRequest()));

        Assert.Equal(
            "ListingBookNotAvailable",
            exception.Message);

        context.ClearTracking();

        Listing storedListing =
            await context.DbContext.Listings
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            4,
            storedListing.Quantity);

        Assert.Empty(
            await context.DbContext.Orders
                .AsNoTracking()
                .ToListAsync());
    }

    [Fact]
    public async Task
        CheckoutAsync_WhenListingPriceIsInvalid_ThrowsInvalidListingPrice()
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser customer =
            await context.CreateUserAsync(
                id: "unit-test-customer-id",
                userName:
                    "invalid-price-checkout-customer");

        ApplicationUser seller =
            await context.CreateUserAsync(
                id: "invalid-price-checkout-seller-id",
                userName:
                    "invalid-price-checkout-seller",
                storeName:
                    "Invalid Price Store",
                isSeller: true);

        var book =
            await context.CreateBookAsync(
                title:
                    "Invalid Price Book");

        Listing listing =
            await context.CreateListingAsync(
                book,
                seller,
                price: -1m,
                quantity: 5);

        Cart cart =
            await context.CreateCartAsync(
                customer);

        await context.CreateCartItemAsync(
            cart,
            listing,
            quantity: 1);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    context.OrderService
                        .CheckoutAsync(
                            new CheckoutRequest()));

        Assert.Equal(
            "InvalidListingPrice",
            exception.Message);

        context.ClearTracking();

        Listing storedListing =
            await context.DbContext.Listings
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            -1m,
            storedListing.Price);

        Assert.Equal(
            5,
            storedListing.Quantity);

        Assert.Empty(
            await context.DbContext.Orders
                .AsNoTracking()
                .ToListAsync());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task
        CheckoutAsync_WhenListingDiscountIsInvalid_ThrowsInvalidListingDiscount(
            decimal discountPercentage)
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser customer =
            await context.CreateUserAsync(
                id: "unit-test-customer-id",
                userName:
                    $"invalid-discount-customer-{discountPercentage}");

        ApplicationUser seller =
            await context.CreateUserAsync(
                id:
                    $"invalid-discount-seller-{discountPercentage}",
                userName:
                    $"invalid-discount-seller-{discountPercentage}",
                storeName:
                    "Invalid Discount Store",
                isSeller: true);

        var book =
            await context.CreateBookAsync(
                title:
                    "Invalid Listing Discount Book");

        Listing listing =
            await context.CreateListingAsync(
                book,
                seller,
                price: 100m,
                quantity: 5,
                discountPercentage:
                    discountPercentage);

        Cart cart =
            await context.CreateCartAsync(
                customer);

        await context.CreateCartItemAsync(
            cart,
            listing,
            quantity: 1);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    context.OrderService
                        .CheckoutAsync(
                            new CheckoutRequest()));

        Assert.Equal(
            "InvalidListingDiscount",
            exception.Message);

        context.ClearTracking();

        Listing storedListing =
            await context.DbContext.Listings
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            5,
            storedListing.Quantity);

        Assert.Equal(
            discountPercentage,
            storedListing.DiscountPercentage);

        Assert.Empty(
            await context.DbContext.Orders
                .AsNoTracking()
                .ToListAsync());
    }
}