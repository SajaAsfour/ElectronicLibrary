using ElectronicLibrary.BLL.Exceptions;
using ElectronicLibrary.DAL.DTOs.Requests.Orders;
using ElectronicLibrary.DAL.DTOs.Responses.Orders;
using ElectronicLibrary.DAL.Enums;
using ElectronicLibrary.DAL.Models.Identity;
using ElectronicLibrary.DAL.Models.Marketplace;
using ElectronicLibrary.DAL.Models.Orders;
using ElectronicLibrary.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ElectronicLibrary.UnitTests.Services.Orders;

public class OrderServiceAdminTests
{
    [Fact]
    public async Task
        GetAllOrdersAsync_ReturnsOrdersForAllCustomers()
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser firstCustomer =
            await context.CreateUserAsync(
                id: "first-admin-customer-id",
                userName: "first-admin-customer");

        ApplicationUser secondCustomer =
            await context.CreateUserAsync(
                id: "second-admin-customer-id",
                userName: "second-admin-customer");

        ApplicationUser seller =
            await context.CreateUserAsync(
                id: "admin-list-seller-id",
                userName: "admin-list-seller",
                storeName: "Admin List Store",
                isSeller: true);

        var book =
            await context.CreateBookAsync(
                title: "Admin List Book");

        Listing listing =
            await context.CreateListingAsync(
                book,
                seller,
                price: 50m,
                quantity: 20);

        Order olderOrder =
            await context.CreateOrderAsync(
                firstCustomer,
                orderDate:
                    DateTime.UtcNow.AddDays(-2));

        await context.CreateOrderItemAsync(
            olderOrder,
            listing,
            quantity: 1);

        Order newerOrder =
            await context.CreateOrderAsync(
                secondCustomer,
                orderDate:
                    DateTime.UtcNow.AddDays(-1));

        await context.CreateOrderItemAsync(
            newerOrder,
            listing,
            quantity: 2);

        var request =
            new OrderFilterRequest
            {
                PageNumber = 1,
                PageSize = 10
            };

        var response =
            await context.OrderService
                .GetAllOrdersAsync(request);

        Assert.Equal(
            2,
            response.TotalCount);

        Assert.Equal(
            1,
            response.TotalPages);

        OrderSummaryResponse[] items =
            response.Items.ToArray();

        Assert.Equal(
            2,
            items.Length);

        Assert.Equal(
            newerOrder.OrderId,
            items[0].OrderId);

        Assert.Equal(
            olderOrder.OrderId,
            items[1].OrderId);

        Assert.Equal(
            2,
            items[0].TotalItems);

        Assert.Equal(
            100m,
            items[0].SubtotalAmount);

        Assert.Equal(
            100m,
            items[0].TotalAmount);
    }

    [Fact]
    public async Task
        GetOrderByIdForAdminAsync_WithExistingOrder_ReturnsOrderRegardlessOfOwner()
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser customer =
            await context.CreateUserAsync(
                id: "admin-details-customer-id",
                userName:
                    "admin-details-customer");

        ApplicationUser seller =
            await context.CreateUserAsync(
                id: "admin-details-seller-id",
                userName:
                    "admin-details-seller",
                storeName:
                    "Admin Details Store",
                isSeller: true);

        var book =
            await context.CreateBookAsync(
                title: "Admin Details Book");

        Listing listing =
            await context.CreateListingAsync(
                book,
                seller,
                price: 100m,
                quantity: 10,
                discountPercentage: 20m);

        Order order =
            await context.CreateOrderAsync(
                customer);

        OrderItem orderItem =
            await context.CreateOrderItemAsync(
                order,
                listing,
                quantity: 2);

        OrderDetailsResponse response =
            await context.OrderService
                .GetOrderByIdForAdminAsync(
                    order.OrderId);

        Assert.Equal(
            order.OrderId,
            response.OrderId);

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
            40m,
            response.ListingDiscountAmount);

        Assert.Equal(
            160m,
            response.TotalAmount);

        OrderItemResponse responseItem =
            Assert.Single(response.Items);

        Assert.Equal(
            orderItem.OrderItemId,
            responseItem.OrderItemId);

        Assert.Equal(
            "Admin Details Book",
            responseItem.BookTitle);

        Assert.Equal(
            "Admin Details Store",
            responseItem.SellerStoreName);

        Assert.Equal(
            160m,
            responseItem.LineTotal);
    }

    [Fact]
    public async Task
        UpdateOrderItemStatusForAdminAsync_FromPendingToConfirmed_UpdatesItemAndOrder()
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser admin =
            await context.CreateUserAsync(
                id: "unit-test-admin-id",
                userName: "order-admin",
                isAdmin: true);

        ApplicationUser customer =
            await context.CreateUserAsync(
                id: "admin-update-customer-id",
                userName:
                    "admin-update-customer");

        ApplicationUser seller =
            await context.CreateUserAsync(
                id: "admin-update-seller-id",
                userName:
                    "admin-update-seller",
                storeName:
                    "Admin Update Store",
                isSeller: true);

        var book =
            await context.CreateBookAsync(
                title: "Admin Update Book");

        Listing listing =
            await context.CreateListingAsync(
                book,
                seller,
                quantity: 5);

        Order order =
            await context.CreateOrderAsync(
                customer,
                status: OrderStatus.Pending);

        OrderItem orderItem =
            await context.CreateOrderItemAsync(
                order,
                listing,
                status:
                    OrderItemStatus.Pending);

        context.ChangeCurrentUser(
            admin.Id);

        var request =
            new UpdateOrderItemStatusRequest
            {
                Status =
                    OrderItemStatus.Confirmed
            };

        OrderDetailsResponse response =
            await context.OrderService
                .UpdateOrderItemStatusForAdminAsync(
                    orderItem.OrderItemId,
                    request);

        Assert.Equal(
            OrderStatus.Confirmed,
            response.Status);

        OrderItemResponse responseItem =
            Assert.Single(response.Items);

        Assert.Equal(
            OrderItemStatus.Confirmed,
            responseItem.Status);

        context.ClearTracking();

        Order storedOrder =
            await context.DbContext.Orders
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            OrderStatus.Confirmed,
            storedOrder.Status);

        OrderItem storedOrderItem =
            await context.DbContext.OrderItems
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            OrderItemStatus.Confirmed,
            storedOrderItem.Status);
    }

    [Fact]
    public async Task
        UpdateOrderItemStatusForAdminAsync_ToCancelled_RestoresStockAndUpdatesAudit()
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser admin =
            await context.CreateUserAsync(
                id: "unit-test-admin-id",
                userName:
                    "cancellation-admin",
                isAdmin: true);

        ApplicationUser customer =
            await context.CreateUserAsync(
                id: "admin-cancel-customer-id",
                userName:
                    "admin-cancel-customer");

        ApplicationUser seller =
            await context.CreateUserAsync(
                id: "admin-cancel-seller-id",
                userName:
                    "admin-cancel-seller",
                storeName:
                    "Admin Cancellation Store",
                isSeller: true);

        var book =
            await context.CreateBookAsync(
                title:
                    "Admin Cancellation Book");

        Listing listing =
            await context.CreateListingAsync(
                book,
                seller,
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

        context.ChangeCurrentUser(
            admin.Id);

        var request =
            new UpdateOrderItemStatusRequest
            {
                Status =
                    OrderItemStatus.Cancelled
            };

        OrderDetailsResponse response =
            await context.OrderService
                .UpdateOrderItemStatusForAdminAsync(
                    orderItem.OrderItemId,
                    request);

        Assert.Equal(
            OrderStatus.Cancelled,
            response.Status);

        OrderItemResponse responseItem =
            Assert.Single(response.Items);

        Assert.Equal(
            OrderItemStatus.Cancelled,
            responseItem.Status);

        context.ClearTracking();

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
            admin.Id,
            storedListing.UpdatedById);

        Order storedOrder =
            await context.DbContext.Orders
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            OrderStatus.Cancelled,
            storedOrder.Status);
    }

    [Fact]
    public async Task
        UpdateOrderItemStatusForAdminAsync_WithInvalidTransition_ThrowsConflictException()
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser admin =
            await context.CreateUserAsync(
                id: "unit-test-admin-id",
                userName:
                    "transition-admin",
                isAdmin: true);

        ApplicationUser customer =
            await context.CreateUserAsync(
                id:
                    "admin-transition-customer-id",
                userName:
                    "admin-transition-customer");

        ApplicationUser seller =
            await context.CreateUserAsync(
                id:
                    "admin-transition-seller-id",
                userName:
                    "admin-transition-seller",
                storeName:
                    "Admin Transition Store",
                isSeller: true);

        var book =
            await context.CreateBookAsync(
                title:
                    "Admin Transition Book");

        Listing listing =
            await context.CreateListingAsync(
                book,
                seller,
                quantity: 5);

        Order order =
            await context.CreateOrderAsync(
                customer,
                status:
                    OrderStatus.Pending);

        OrderItem orderItem =
            await context.CreateOrderItemAsync(
                order,
                listing,
                status:
                    OrderItemStatus.Pending);

        context.ChangeCurrentUser(
            admin.Id);

        var request =
            new UpdateOrderItemStatusRequest
            {
                Status =
                    OrderItemStatus.Delivered
            };

        ConflictException exception =
            await Assert.ThrowsAsync<
                ConflictException>(
                () =>
                    context.OrderService
                        .UpdateOrderItemStatusForAdminAsync(
                            orderItem.OrderItemId,
                            request));

        Assert.Equal(
            "InvalidOrderItemStatusTransition",
            exception.Message);

        context.ClearTracking();

        OrderItem storedOrderItem =
            await context.DbContext.OrderItems
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            OrderItemStatus.Pending,
            storedOrderItem.Status);

        Order storedOrder =
            await context.DbContext.Orders
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            OrderStatus.Pending,
            storedOrder.Status);
    }
}