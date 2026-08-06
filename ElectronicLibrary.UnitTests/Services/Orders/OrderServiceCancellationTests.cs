using ElectronicLibrary.BLL.Exceptions;
using ElectronicLibrary.DAL.DTOs.Responses.Orders;
using ElectronicLibrary.DAL.Enums;
using ElectronicLibrary.DAL.Models.Identity;
using ElectronicLibrary.DAL.Models.Marketplace;
using ElectronicLibrary.DAL.Models.Orders;
using ElectronicLibrary.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ElectronicLibrary.UnitTests.Services.Orders;

public class OrderServiceCancellationTests
{
    [Fact]
    public async Task
        CancelCurrentUserOrderAsync_WithPendingOrder_CancelsItemsAndRestoresStock()
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser customer =
            await context.CreateUserAsync(
                id: "unit-test-customer-id",
                userName:
                    "cancel-order-customer");

        ApplicationUser seller =
            await context.CreateUserAsync(
                id: "cancel-order-seller-id",
                userName:
                    "cancel-order-seller",
                storeName:
                    "Cancellation Store",
                isSeller: true);

        var book =
            await context.CreateBookAsync(
                title:
                    "Cancelled Order Book");

        Listing listing =
            await context.CreateListingAsync(
                book,
                seller,
                price: 100m,
                quantity: 0,
                status:
                    ListingStatus.OutOfStock);

        Order order =
            await context.CreateOrderAsync(
                customer,
                status:
                    OrderStatus.Pending);

        OrderItem orderItem =
            await context.CreateOrderItemAsync(
                order,
                listing,
                quantity: 2,
                status:
                    OrderItemStatus.Pending);

        OrderDetailsResponse response =
            await context.OrderService
                .CancelCurrentUserOrderAsync(
                    order.OrderId);

        Assert.Equal(
            OrderStatus.Cancelled,
            response.Status);

        OrderItemResponse responseItem =
            Assert.Single(response.Items);

        Assert.Equal(
            orderItem.OrderItemId,
            responseItem.OrderItemId);

        Assert.Equal(
            OrderItemStatus.Cancelled,
            responseItem.Status);

        context.ClearTracking();

        Order storedOrder =
            await context.DbContext.Orders
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            OrderStatus.Cancelled,
            storedOrder.Status);

        OrderItem storedOrderItem =
            await context.DbContext.OrderItems
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            OrderItemStatus.Cancelled,
            storedOrderItem.Status);

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

        Assert.NotNull(
            storedListing.UpdatedAt);

        Assert.Equal(
            customer.Id,
            storedListing.UpdatedById);
    }

    [Fact]
    public async Task
        CancelCurrentUserOrderAsync_WhenOrderHasProcessingItem_ThrowsOrderCannotBeCancelled()
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser customer =
            await context.CreateUserAsync(
                id: "unit-test-customer-id",
                userName:
                    "processing-order-customer");

        ApplicationUser seller =
            await context.CreateUserAsync(
                id:
                    "processing-order-seller-id",
                userName:
                    "processing-order-seller",
                storeName:
                    "Processing Order Store",
                isSeller: true);

        var book =
            await context.CreateBookAsync(
                title:
                    "Processing Order Book");

        Listing listing =
            await context.CreateListingAsync(
                book,
                seller,
                quantity: 3);

        Order order =
            await context.CreateOrderAsync(
                customer,
                status:
                    OrderStatus.Processing);

        await context.CreateOrderItemAsync(
            order,
            listing,
            quantity: 1,
            status:
                OrderItemStatus.Processing);

        ConflictException exception =
            await Assert.ThrowsAsync<
                ConflictException>(
                () =>
                    context.OrderService
                        .CancelCurrentUserOrderAsync(
                            order.OrderId));

        Assert.Equal(
            "OrderCannotBeCancelled",
            exception.Message);

        context.ClearTracking();

        Order storedOrder =
            await context.DbContext.Orders
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            OrderStatus.Processing,
            storedOrder.Status);

        OrderItem storedOrderItem =
            await context.DbContext.OrderItems
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            OrderItemStatus.Processing,
            storedOrderItem.Status);

        Listing storedListing =
            await context.DbContext.Listings
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            3,
            storedListing.Quantity);
    }

    [Fact]
    public async Task
        CancelCurrentUserOrderAsync_WhenOrderBelongsToAnotherUser_ThrowsOrderNotFound()
    {
        await using var context =
            new OrderServiceTestContext();

        await context.CreateUserAsync(
            id: "unit-test-customer-id",
            userName:
                "non-owner-customer");

        ApplicationUser orderOwner =
            await context.CreateUserAsync(
                id: "order-owner-id",
                userName:
                    "actual-order-owner");

        Order order =
            await context.CreateOrderAsync(
                orderOwner);

        KeyNotFoundException exception =
            await Assert.ThrowsAsync<
                KeyNotFoundException>(
                () =>
                    context.OrderService
                        .CancelCurrentUserOrderAsync(
                            order.OrderId));

        Assert.Equal(
            "OrderNotFound",
            exception.Message);

        Order storedOrder =
            await context.DbContext.Orders
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            OrderStatus.Pending,
            storedOrder.Status);
    }

    [Fact]
    public async Task
        CancelCurrentUserOrderAsync_WhenOrderIsDelivered_ThrowsDeliveredOrderCannotBeCancelled()
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser customer =
            await context.CreateUserAsync(
                id: "unit-test-customer-id",
                userName:
                    "delivered-order-customer");

        Order order =
            await context.CreateOrderAsync(
                customer,
                status:
                    OrderStatus.Delivered);

        ConflictException exception =
            await Assert.ThrowsAsync<
                ConflictException>(
                () =>
                    context.OrderService
                        .CancelCurrentUserOrderAsync(
                            order.OrderId));

        Assert.Equal(
            "DeliveredOrderCannotBeCancelled",
            exception.Message);

        Order storedOrder =
            await context.DbContext.Orders
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            OrderStatus.Delivered,
            storedOrder.Status);
    }

    [Fact]
    public async Task
        CancelCurrentUserOrderAsync_WhenOrderAlreadyCancelled_ThrowsOrderAlreadyCancelled()
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser customer =
            await context.CreateUserAsync(
                id: "unit-test-customer-id",
                userName:
                    "already-cancelled-customer");

        Order order =
            await context.CreateOrderAsync(
                customer,
                status:
                    OrderStatus.Cancelled);

        ConflictException exception =
            await Assert.ThrowsAsync<
                ConflictException>(
                () =>
                    context.OrderService
                        .CancelCurrentUserOrderAsync(
                            order.OrderId));

        Assert.Equal(
            "OrderAlreadyCancelled",
            exception.Message);

        Order storedOrder =
            await context.DbContext.Orders
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            OrderStatus.Cancelled,
            storedOrder.Status);
    }
}
