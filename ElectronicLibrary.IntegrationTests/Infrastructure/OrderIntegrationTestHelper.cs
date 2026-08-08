using ElectronicLibrary.DAL.Constants;
using ElectronicLibrary.DAL.Data;
using ElectronicLibrary.DAL.Enums;
using ElectronicLibrary.DAL.Models.Discounts;
using ElectronicLibrary.DAL.Models.Marketplace;
using ElectronicLibrary.DAL.Models.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ElectronicLibrary.IntegrationTests.Infrastructure;

public static class OrderIntegrationTestHelper
{
    public static async Task<TestCheckoutContext>
        CreateCheckoutContextAsync(
            CustomWebApplicationFactory factory,
            decimal price = 100m,
            int stockQuantity = 5,
            int cartQuantity = 2,
            decimal discountPercentage = 0m,
            ListingStatus listingStatus =
                ListingStatus.Active)
    {
        CartIntegrationTestHelper
            .TestCartMarketplaceContext marketplace =
                await CartIntegrationTestHelper
                    .CreateMarketplaceContextAsync(
                        factory,
                        price: price,
                        quantity: stockQuantity,
                        discountPercentage:
                            discountPercentage,
                        status: listingStatus);

        CartIntegrationTestHelper.TestCartContext cart =
            await CartIntegrationTestHelper
                .CreateCartAsync(
                    factory,
                    marketplace.Customer.UserId);

        CartIntegrationTestHelper.TestCartItemContext
            cartItem =
                await CartIntegrationTestHelper
                    .CreateCartItemAsync(
                        factory,
                        cart.CartId,
                        marketplace.Listing.ListingId,
                        cartQuantity);

        return new TestCheckoutContext(
            marketplace.Customer,
            marketplace.Seller,
            marketplace.Book,
            marketplace.Listing,
            cart,
            cartItem);
    }

    public static async Task<
        ListingIntegrationTestHelper.TestUserContext>
        CreateAdminAsync(
            CustomWebApplicationFactory factory)
    {
        return await ListingIntegrationTestHelper
            .CreateAuthenticatedUserAsync(
                factory,
                ApplicationRoles.Admin);
    }

    public static async Task<TestCouponContext>
        CreateCouponAsync(
            CustomWebApplicationFactory factory,
            string? code = null,
            decimal discountValue = 10m,
            string discountType = "Percentage",
            bool isActive = true,
            DateTime? startDate = null,
            DateTime? endDate = null)
    {
        using IServiceScope scope =
            factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    ApplicationDbContext>();

        string resolvedCode =
            code ??
            $"ORDER-{Guid.NewGuid():N}"[..30]
                .ToUpperInvariant();

        var coupon = new Coupon
        {
            Code = resolvedCode,
            DiscountValue =
                discountValue,
            DiscountType =
                discountType,
            IsActive =
                isActive,
            StartDate =
                startDate ??
                DateTime.UtcNow.AddDays(-1),
            EndDate =
                endDate ??
                DateTime.UtcNow.AddDays(7)
        };

        dbContext.Coupons.Add(coupon);

        await dbContext.SaveChangesAsync();

        return new TestCouponContext(
            coupon.CouponId,
            coupon.Code,
            coupon.DiscountValue,
            coupon.DiscountType);
    }

    public static async Task<TestOrderContext>
        CreateOrderAsync(
            CustomWebApplicationFactory factory,
            string customerId,
            OrderStatus status =
                OrderStatus.Pending,
            decimal subtotalAmount = 100m,
            decimal listingDiscountAmount = 0m,
            decimal couponDiscountAmount = 0m,
            int? couponId = null,
            string? couponCodeSnapshot = null,
            string? couponDiscountTypeSnapshot = null,
            decimal? couponDiscountValueSnapshot = null,
            DateTime? orderDate = null)
    {
        using IServiceScope scope =
            factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    ApplicationDbContext>();

        decimal totalDiscountAmount =
            listingDiscountAmount +
            couponDiscountAmount;

        decimal totalAmount =
            subtotalAmount -
            totalDiscountAmount;

        var order = new Order
        {
            UserId = customerId,
            OrderDate =
                orderDate ??
                DateTime.UtcNow,
            Status = status,
            SubtotalAmount =
                subtotalAmount,
            ListingDiscountAmount =
                listingDiscountAmount,
            CouponDiscountAmount =
                couponDiscountAmount,
            TotalDiscountAmount =
                totalDiscountAmount,
            TotalAmount =
                totalAmount,
            CouponId =
                couponId,
            CouponCodeSnapshot =
                couponCodeSnapshot,
            CouponDiscountTypeSnapshot =
                couponDiscountTypeSnapshot,
            CouponDiscountValueSnapshot =
                couponDiscountValueSnapshot
        };

        dbContext.Orders.Add(order);

        await dbContext.SaveChangesAsync();

        return new TestOrderContext(
            order.OrderId,
            order.UserId,
            order.Status);
    }

    public static async Task<TestOrderItemContext>
        CreateOrderItemAsync(
            CustomWebApplicationFactory factory,
            int orderId,
            int listingId,
            int quantity = 1,
            OrderItemStatus status =
                OrderItemStatus.Pending,
            decimal? unitPrice = null,
            decimal? discountPercentage = null)
    {
        using IServiceScope scope =
            factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    ApplicationDbContext>();

        Listing listing =
            await dbContext.Listings
                .Include(currentListing =>
                    currentListing.Book)
                .Include(currentListing =>
                    currentListing.Seller)
                .SingleAsync(
                    currentListing =>
                        currentListing.ListingId ==
                        listingId);

        decimal resolvedUnitPrice =
            unitPrice ??
            listing.Price;

        decimal resolvedDiscountPercentage =
            discountPercentage ??
            listing.DiscountPercentage;

        decimal effectiveUnitPrice =
            decimal.Round(
                resolvedUnitPrice *
                (1m -
                    resolvedDiscountPercentage /
                    100m),
                2,
                MidpointRounding.AwayFromZero);

        decimal lineSubtotal =
            decimal.Round(
                resolvedUnitPrice *
                quantity,
                2,
                MidpointRounding.AwayFromZero);

        decimal lineTotal =
            decimal.Round(
                effectiveUnitPrice *
                quantity,
                2,
                MidpointRounding.AwayFromZero);

        decimal lineDiscount =
            lineSubtotal -
            lineTotal;

        var orderItem = new OrderItem
        {
            OrderId = orderId,
            ListingId =
                listing.ListingId,
            BookId =
                listing.BookId,
            SellerId =
                listing.SellerId,
            BookTitleSnapshot =
                listing.Book.Title,
            SellerStoreNameSnapshot =
                listing.Seller.StoreName ??
                "Integration Test Store",
            FormatSnapshot =
                listing.Format,
            ConditionSnapshot =
                listing.Condition,
            Quantity =
                quantity,
            UnitPrice =
                resolvedUnitPrice,
            DiscountPercentage =
                resolvedDiscountPercentage,
            EffectiveUnitPrice =
                effectiveUnitPrice,
            LineSubtotal =
                lineSubtotal,
            LineDiscount =
                lineDiscount,
            LineTotal =
                lineTotal,
            Status =
                status
        };

        dbContext.OrderItems.Add(orderItem);

        Order order =
            await dbContext.Orders
                .SingleAsync(
                    currentOrder =>
                        currentOrder.OrderId ==
                        orderId);

        order.SubtotalAmount +=
            lineSubtotal;

        order.ListingDiscountAmount +=
            lineDiscount;

        order.TotalDiscountAmount =
            order.ListingDiscountAmount +
            order.CouponDiscountAmount;

        order.TotalAmount =
            order.SubtotalAmount -
            order.TotalDiscountAmount;

        await dbContext.SaveChangesAsync();

        return new TestOrderItemContext(
            orderItem.OrderItemId,
            orderItem.OrderId,
            orderItem.ListingId,
            orderItem.BookId,
            orderItem.SellerId,
            orderItem.Status);
    }

    public static async Task<Order>
        GetOrderAsync(
            CustomWebApplicationFactory factory,
            int orderId)
    {
        using IServiceScope scope =
            factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    ApplicationDbContext>();

        return await dbContext.Orders
            .AsNoTracking()
            .Include(order =>
                order.OrderItems)
            .SingleAsync(
                order =>
                    order.OrderId ==
                    orderId);
    }

    public static async Task<OrderItem>
        GetOrderItemAsync(
            CustomWebApplicationFactory factory,
            int orderItemId)
    {
        using IServiceScope scope =
            factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    ApplicationDbContext>();

        return await dbContext.OrderItems
            .AsNoTracking()
            .SingleAsync(
                item =>
                    item.OrderItemId ==
                    orderItemId);
    }

    public static async Task<Coupon>
        GetCouponAsync(
            CustomWebApplicationFactory factory,
            int couponId)
    {
        using IServiceScope scope =
            factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    ApplicationDbContext>();

        return await dbContext.Coupons
            .AsNoTracking()
            .SingleAsync(
                coupon =>
                    coupon.CouponId ==
                    couponId);
    }

    public static async Task<int>
        GetOrderCountForUserAsync(
            CustomWebApplicationFactory factory,
            string userId)
    {
        using IServiceScope scope =
            factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    ApplicationDbContext>();

        return await dbContext.Orders
            .AsNoTracking()
            .CountAsync(
                order =>
                    order.UserId ==
                    userId);
    }

    public static async Task<int>
        GetOrderItemCountForOrderAsync(
            CustomWebApplicationFactory factory,
            int orderId)
    {
        using IServiceScope scope =
            factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<
                    ApplicationDbContext>();

        return await dbContext.OrderItems
            .AsNoTracking()
            .CountAsync(
                item =>
                    item.OrderId ==
                    orderId);
    }

    public sealed record TestCheckoutContext(
        ListingIntegrationTestHelper
            .TestUserContext Customer,
        ListingIntegrationTestHelper
            .TestUserContext Seller,
        ListingIntegrationTestHelper
            .TestBookContext Book,
        ListingIntegrationTestHelper
            .TestListingContext Listing,
        CartIntegrationTestHelper
            .TestCartContext Cart,
        CartIntegrationTestHelper
            .TestCartItemContext CartItem);

    public sealed record TestCouponContext(
        int CouponId,
        string Code,
        decimal DiscountValue,
        string DiscountType);

    public sealed record TestOrderContext(
        int OrderId,
        string UserId,
        OrderStatus Status);

    public sealed record TestOrderItemContext(
        int OrderItemId,
        int OrderId,
        int ListingId,
        int BookId,
        string SellerId,
        OrderItemStatus Status);
}
