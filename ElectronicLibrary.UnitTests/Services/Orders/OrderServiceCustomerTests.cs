using ElectronicLibrary.DAL.DTOs.Requests.Orders;
using ElectronicLibrary.DAL.DTOs.Responses.Orders;
using ElectronicLibrary.DAL.Enums;
using ElectronicLibrary.DAL.Models.Identity;
using ElectronicLibrary.DAL.Models.Marketplace;
using ElectronicLibrary.DAL.Models.Orders;
using ElectronicLibrary.UnitTests.Helpers;

namespace ElectronicLibrary.UnitTests.Services.Orders;

public class OrderServiceCustomerTests
{
    [Fact]
    public async Task
        GetCurrentUserOrdersAsync_ReturnsOnlyCurrentUserOrdersOrderedByNewest()
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser currentCustomer =
            await context.CreateUserAsync(
                id: "unit-test-customer-id",
                userName: "current-order-customer");

        ApplicationUser otherCustomer =
            await context.CreateUserAsync(
                id: "other-order-customer-id",
                userName: "other-order-customer");

        ApplicationUser seller =
            await context.CreateUserAsync(
                id: "customer-orders-seller-id",
                userName: "customer-orders-seller",
                storeName: "Customer Orders Store",
                isSeller: true);

        var book =
            await context.CreateBookAsync(
                title: "Customer Orders Book");

        Listing listing =
            await context.CreateListingAsync(
                book,
                seller,
                price: 50m,
                quantity: 20);

        Order olderOrder =
            await context.CreateOrderAsync(
                currentCustomer,
                orderDate:
                    DateTime.UtcNow.AddDays(-2));

        await context.CreateOrderItemAsync(
            olderOrder,
            listing,
            quantity: 1);

        Order newestOrder =
            await context.CreateOrderAsync(
                currentCustomer,
                orderDate:
                    DateTime.UtcNow.AddDays(-1));

        await context.CreateOrderItemAsync(
            newestOrder,
            listing,
            quantity: 2);

        Order otherCustomerOrder =
            await context.CreateOrderAsync(
                otherCustomer,
                orderDate: DateTime.UtcNow);

        await context.CreateOrderItemAsync(
            otherCustomerOrder,
            listing,
            quantity: 3);

        var request =
            new OrderFilterRequest
            {
                PageNumber = 1,
                PageSize = 10
            };

        var response =
            await context.OrderService
                .GetCurrentUserOrdersAsync(
                    request);

        Assert.Equal(
            2,
            response.TotalCount);

        Assert.Equal(
            1,
            response.TotalPages);

        Assert.False(
            response.HasPreviousPage);

        Assert.False(
            response.HasNextPage);

        OrderSummaryResponse[] items =
            response.Items.ToArray();

        Assert.Equal(
            2,
            items.Length);

        Assert.Equal(
            newestOrder.OrderId,
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

        Assert.DoesNotContain(
            items,
            item =>
                item.OrderId ==
                otherCustomerOrder.OrderId);
    }

    [Fact]
    public async Task
        GetCurrentUserOrderByIdAsync_WithOwnedOrder_ReturnsOrderDetailsAndSnapshots()
    {
        await using var context =
            new OrderServiceTestContext();

        ApplicationUser customer =
            await context.CreateUserAsync(
                id: "unit-test-customer-id",
                userName:
                    "owned-order-customer");

        ApplicationUser seller =
            await context.CreateUserAsync(
                id: "owned-order-seller-id",
                userName:
                    "owned-order-seller",
                storeName: "Owned Order Store",
                isSeller: true);

        var book =
            await context.CreateBookAsync(
                title: "Owned Order Book");

        Listing listing =
            await context.CreateListingAsync(
                book,
                seller,
                price: 120m,
                quantity: 10,
                discountPercentage: 25m,
                format: BookFormat.Physical,
                condition: BookCondition.Good);

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
                .GetCurrentUserOrderByIdAsync(
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
            240m,
            response.SubtotalAmount);

        Assert.Equal(
            60m,
            response.ListingDiscountAmount);

        Assert.Equal(
            180m,
            response.TotalAmount);

        OrderItemResponse responseItem =
            Assert.Single(response.Items);

        Assert.Equal(
            orderItem.OrderItemId,
            responseItem.OrderItemId);

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
            "Owned Order Book",
            responseItem.BookTitle);

        Assert.Equal(
            "Owned Order Store",
            responseItem.SellerStoreName);

        Assert.Equal(
            BookFormat.Physical,
            responseItem.Format);

        Assert.Equal(
            BookCondition.Good,
            responseItem.Condition);

        Assert.Equal(
            120m,
            responseItem.UnitPrice);

        Assert.Equal(
            25m,
            responseItem.DiscountPercentage);

        Assert.Equal(
            90m,
            responseItem.EffectiveUnitPrice);

        Assert.Equal(
            180m,
            responseItem.LineTotal);
    }

    [Fact]
    public async Task
        GetCurrentUserOrderByIdAsync_WithAnotherUserOrder_ThrowsOrderNotFound()
    {
        await using var context =
            new OrderServiceTestContext();

        await context.CreateUserAsync(
            id: "unit-test-customer-id",
            userName:
                "unauthorized-order-customer");

        ApplicationUser otherCustomer =
            await context.CreateUserAsync(
                id: "other-customer-id",
                userName:
                    "order-owner-customer");

        Order otherCustomerOrder =
            await context.CreateOrderAsync(
                otherCustomer);

        KeyNotFoundException exception =
            await Assert.ThrowsAsync<
                KeyNotFoundException>(
                () =>
                    context.OrderService
                        .GetCurrentUserOrderByIdAsync(
                            otherCustomerOrder
                                .OrderId));

        Assert.Equal(
            "OrderNotFound",
            exception.Message);
    }
}
