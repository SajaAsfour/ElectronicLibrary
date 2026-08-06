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

public class OrderServiceSellerTests
{
    [Fact]
    public async Task
        GetCurrentSellerOrderItemsAsync_ReturnsOnlyCurrentSellerItems()
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser customer =
            await context.CreateUserAsync(
                id: "seller-items-customer-id",
                userName: "seller-items-customer");

        ApplicationUser currentSeller =
            await context.CreateUserAsync(
                id: "current-order-seller-id",
                userName: "current-order-seller",
                storeName: "Current Seller Store",
                isSeller: true);

        ApplicationUser otherSeller =
            await context.CreateUserAsync(
                id: "other-order-seller-id",
                userName: "other-order-seller",
                storeName: "Other Seller Store",
                isSeller: true);

        var currentSellerBook =
            await context.CreateBookAsync(
                title: "Current Seller Book");

        var otherSellerBook =
            await context.CreateBookAsync(
                title: "Other Seller Book");

        Listing currentSellerListing =
            await context.CreateListingAsync(
                currentSellerBook,
                currentSeller,
                price: 100m,
                quantity: 10);

        Listing otherSellerListing =
            await context.CreateListingAsync(
                otherSellerBook,
                otherSeller,
                price: 80m,
                quantity: 10);

        Order order =
            await context.CreateOrderAsync(
                customer);

        OrderItem currentSellerItem =
            await context.CreateOrderItemAsync(
                order,
                currentSellerListing,
                quantity: 2);

        OrderItem otherSellerItem =
            await context.CreateOrderItemAsync(
                order,
                otherSellerListing,
                quantity: 3);

        context.ChangeCurrentUser(
            currentSeller.Id);

        var request =
            new SellerOrderItemFilterRequest
            {
                PageNumber = 1,
                PageSize = 10
            };

        var response =
            await context.OrderService
                .GetCurrentSellerOrderItemsAsync(
                    request);

        Assert.Equal(
            1,
            response.TotalCount);

        SellerOrderItemResponse responseItem =
            Assert.Single(response.Items);

        Assert.Equal(
            currentSellerItem.OrderItemId,
            responseItem.OrderItemId);

        Assert.Equal(
            order.OrderId,
            responseItem.OrderId);

        Assert.Equal(
            currentSeller.Id,
            responseItem.SellerId);

        Assert.Equal(
            "Current Seller Book",
            responseItem.BookTitle);

        Assert.Equal(
            "Current Seller Store",
            responseItem.SellerStoreName);

        Assert.Equal(
            2,
            responseItem.Quantity);

        Assert.Equal(
            200m,
            responseItem.LineTotal);

        Assert.DoesNotContain(
            response.Items,
            item =>
                item.OrderItemId ==
                otherSellerItem.OrderItemId);
    }

    [Fact]
    public async Task
        UpdateCurrentSellerOrderItemStatusAsync_FromPendingToConfirmed_UpdatesItemAndOrderStatus()
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser customer =
            await context.CreateUserAsync(
                id: "confirm-item-customer-id",
                userName: "confirm-item-customer");

        ApplicationUser seller =
            await context.CreateUserAsync(
                id: "confirm-item-seller-id",
                userName: "confirm-item-seller",
                storeName: "Confirmation Store",
                isSeller: true);

        var book =
            await context.CreateBookAsync(
                title: "Confirmation Book");

        Listing listing =
            await context.CreateListingAsync(
                book,
                seller,
                price: 60m,
                quantity: 5);

        Order order =
            await context.CreateOrderAsync(
                customer,
                status: OrderStatus.Pending);

        OrderItem orderItem =
            await context.CreateOrderItemAsync(
                order,
                listing,
                quantity: 1,
                status: OrderItemStatus.Pending);

        context.ChangeCurrentUser(
            seller.Id);

        var request =
            new UpdateOrderItemStatusRequest
            {
                Status =
                    OrderItemStatus.Confirmed
            };

        SellerOrderItemResponse response =
            await context.OrderService
                .UpdateCurrentSellerOrderItemStatusAsync(
                    orderItem.OrderItemId,
                    request);

        Assert.Equal(
            OrderItemStatus.Confirmed,
            response.Status);

        Assert.Equal(
            orderItem.OrderItemId,
            response.OrderItemId);

        context.ClearTracking();

        OrderItem storedOrderItem =
            await context.DbContext.OrderItems
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            OrderItemStatus.Confirmed,
            storedOrderItem.Status);

        Order storedOrder =
            await context.DbContext.Orders
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            OrderStatus.Confirmed,
            storedOrder.Status);
    }

    [Fact]
    public async Task
        UpdateCurrentSellerOrderItemStatusAsync_WithAnotherSellerItem_ThrowsOrderItemNotFound()
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser customer =
            await context.CreateUserAsync(
                id: "ownership-customer-id",
                userName: "ownership-customer");

        ApplicationUser currentSeller =
            await context.CreateUserAsync(
                id: "ownership-current-seller-id",
                userName: "ownership-current-seller",
                storeName: "Current Ownership Store",
                isSeller: true);

        ApplicationUser otherSeller =
            await context.CreateUserAsync(
                id: "ownership-other-seller-id",
                userName: "ownership-other-seller",
                storeName: "Other Ownership Store",
                isSeller: true);

        var book =
            await context.CreateBookAsync(
                title: "Ownership Book");

        Listing listing =
            await context.CreateListingAsync(
                book,
                otherSeller,
                quantity: 5);

        Order order =
            await context.CreateOrderAsync(
                customer);

        OrderItem orderItem =
            await context.CreateOrderItemAsync(
                order,
                listing);

        context.ChangeCurrentUser(
            currentSeller.Id);

        var request =
            new UpdateOrderItemStatusRequest
            {
                Status =
                    OrderItemStatus.Confirmed
            };

        KeyNotFoundException exception =
            await Assert.ThrowsAsync<
                KeyNotFoundException>(
                () =>
                    context.OrderService
                        .UpdateCurrentSellerOrderItemStatusAsync(
                            orderItem.OrderItemId,
                            request));

        Assert.Equal(
            "OrderItemNotFound",
            exception.Message);

        OrderItem storedOrderItem =
            await context.DbContext.OrderItems
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            OrderItemStatus.Pending,
            storedOrderItem.Status);
    }

    [Fact]
    public async Task
        UpdateCurrentSellerOrderItemStatusAsync_WithInvalidTransition_ThrowsConflictException()
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser customer =
            await context.CreateUserAsync(
                id: "transition-customer-id",
                userName: "transition-customer");

        ApplicationUser seller =
            await context.CreateUserAsync(
                id: "transition-seller-id",
                userName: "transition-seller",
                storeName: "Transition Store",
                isSeller: true);

        var book =
            await context.CreateBookAsync(
                title: "Transition Book");

        Listing listing =
            await context.CreateListingAsync(
                book,
                seller,
                quantity: 5);

        Order order =
            await context.CreateOrderAsync(
                customer);

        OrderItem orderItem =
            await context.CreateOrderItemAsync(
                order,
                listing,
                status:
                    OrderItemStatus.Pending);

        context.ChangeCurrentUser(
            seller.Id);

        var request =
            new UpdateOrderItemStatusRequest
            {
                Status =
                    OrderItemStatus.Shipped
            };

        ConflictException exception =
            await Assert.ThrowsAsync<
                ConflictException>(
                () =>
                    context.OrderService
                        .UpdateCurrentSellerOrderItemStatusAsync(
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
