using ElectronicLibrary.DAL.DTOs.Requests.Orders;
using ElectronicLibrary.DAL.DTOs.Responses.Common;
using ElectronicLibrary.DAL.DTOs.Responses.Orders;
using ElectronicLibrary.DAL.Enums;
using ElectronicLibrary.DAL.Models.Marketplace;
using ElectronicLibrary.DAL.Models.Orders;
using ElectronicLibrary.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace ElectronicLibrary.IntegrationTests.Controllers;

[Collection(IntegrationTestCollection.Name)]
public sealed class AdminOrdersControllerTests
{
    private readonly CustomWebApplicationFactory
        _factory;

    private readonly HttpClient _client;

    public AdminOrdersControllerTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task
        GetAllOrders_WithoutToken_ReturnsUnauthorized()
    {
        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/admin/orders");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task
        GetAllOrders_WithCustomerToken_ReturnsForbidden()
    {
        ListingIntegrationTestHelper.TestUserContext
            customer =
                await ListingIntegrationTestHelper
                    .CreateCustomerAsync(_factory);

        SetToken(
            customer.AccessToken);

        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/admin/orders");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task
    GetAllOrders_WithStatusFilter_ReturnsMatchingOrders()
    {
        ListingIntegrationTestHelper.TestUserContext
            admin =
                await OrderIntegrationTestHelper
                    .CreateAdminAsync(_factory);

        ListingIntegrationTestHelper.TestUserContext
            customer =
                await ListingIntegrationTestHelper
                    .CreateCustomerAsync(_factory);

        ListingIntegrationTestHelper.TestUserContext
            seller =
                await ListingIntegrationTestHelper
                    .CreateSellerAsync(_factory);

        ListingIntegrationTestHelper.TestBookContext
            book =
                await ListingIntegrationTestHelper
                    .CreateBookAsync(_factory);

        ListingIntegrationTestHelper.TestListingContext
            listing =
                await ListingIntegrationTestHelper
                    .CreateListingAsync(
                        _factory,
                        seller.UserId,
                        book.BookId,
                        price: 50m,
                        quantity: 20,
                        status:
                            ListingStatus.Active);

        OrderIntegrationTestHelper.TestOrderContext
            pendingOrder =
                await OrderIntegrationTestHelper
                    .CreateOrderAsync(
                        _factory,
                        customer.UserId,
                        status:
                            OrderStatus.Pending,
                        subtotalAmount: 0m,
                        orderDate:
                            DateTime.UtcNow.AddDays(-2));

        await OrderIntegrationTestHelper
            .CreateOrderItemAsync(
                _factory,
                pendingOrder.OrderId,
                listing.ListingId,
                quantity: 1,
                status:
                    OrderItemStatus.Pending);

        OrderIntegrationTestHelper.TestOrderContext
            confirmedOrder =
                await OrderIntegrationTestHelper
                    .CreateOrderAsync(
                        _factory,
                        customer.UserId,
                        status:
                            OrderStatus.Confirmed,
                        subtotalAmount: 0m,
                        orderDate:
                            DateTime.UtcNow.AddDays(-1));

        await OrderIntegrationTestHelper
            .CreateOrderItemAsync(
                _factory,
                confirmedOrder.OrderId,
                listing.ListingId,
                quantity: 2,
                status:
                    OrderItemStatus.Confirmed);

        SetToken(
            admin.AccessToken);

        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/admin/orders" +
                $"?status={(int)OrderStatus.Confirmed}" +
                "&sortBy=orderDate" +
                "&sortDirection=desc" +
                "&pageNumber=1" +
                "&pageSize=50");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.OK);

        PagedResponse<OrderSummaryResponse>? result =
            await response.Content
                .ReadFromJsonAsync<
                    PagedResponse<
                        OrderSummaryResponse>>();

        Assert.NotNull(result);

        Assert.True(
            result.TotalCount >= 1);

        Assert.All(
            result.Items,
            order =>
                Assert.Equal(
                    OrderStatus.Confirmed,
                    order.Status));

        OrderSummaryResponse responseOrder =
             Assert.Single(
                 result.Items,
                 order =>
                     order.OrderId ==
                     confirmedOrder.OrderId);

        Assert.Equal(
            OrderStatus.Confirmed,
            responseOrder.Status);

        Assert.Equal(
            2,
            responseOrder.TotalItems);

        Assert.Equal(
            100m,
            responseOrder.SubtotalAmount);

        Assert.Equal(
            0m,
            responseOrder.ListingDiscountAmount);

        Assert.Equal(
            0m,
            responseOrder.CouponDiscountAmount);

        Assert.Equal(
            0m,
            responseOrder.TotalDiscountAmount);

        Assert.Equal(
            100m,
            responseOrder.TotalAmount);

        Assert.DoesNotContain(
            result.Items,
            order =>
                order.OrderId ==
                pendingOrder.OrderId);
    }


    [Fact]
    public async Task
        GetOrderById_WithAdminToken_ReturnsOrderDetails()
    {
        ListingIntegrationTestHelper.TestUserContext
            admin =
                await OrderIntegrationTestHelper
                    .CreateAdminAsync(_factory);

        ListingIntegrationTestHelper.TestUserContext
            customer =
                await ListingIntegrationTestHelper
                    .CreateCustomerAsync(_factory);

        ListingIntegrationTestHelper.TestUserContext
            seller =
                await ListingIntegrationTestHelper
                    .CreateSellerAsync(
                        _factory,
                        "Admin Details Store");

        ListingIntegrationTestHelper.TestBookContext
            book =
                await ListingIntegrationTestHelper
                    .CreateBookAsync(_factory);

        ListingIntegrationTestHelper.TestListingContext
            listing =
                await ListingIntegrationTestHelper
                    .CreateListingAsync(
                        _factory,
                        seller.UserId,
                        book.BookId,
                        price: 120m,
                        quantity: 10,
                        discountPercentage: 10m,
                        status:
                            ListingStatus.Active);

        OrderIntegrationTestHelper.TestOrderContext
            order =
                await OrderIntegrationTestHelper
                    .CreateOrderAsync(
                        _factory,
                        customer.UserId,
                        subtotalAmount: 0m);

        OrderIntegrationTestHelper.TestOrderItemContext
            orderItem =
                await OrderIntegrationTestHelper
                    .CreateOrderItemAsync(
                        _factory,
                        order.OrderId,
                        listing.ListingId,
                        quantity: 2);

        SetToken(
            admin.AccessToken);

        HttpResponseMessage response =
            await _client.GetAsync(
                $"/api/admin/orders/{order.OrderId}");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.OK);

        OrderDetailsResponse? result =
            await response.Content
                .ReadFromJsonAsync<
                    OrderDetailsResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            order.OrderId,
            result.OrderId);

        Assert.Equal(
            customer.UserId,
            result.UserId);

        Assert.Equal(
            OrderStatus.Pending,
            result.Status);

        Assert.Equal(
            2,
            result.TotalItems);

        Assert.Equal(
            240m,
            result.SubtotalAmount);

        Assert.Equal(
            24m,
            result.ListingDiscountAmount);

        Assert.Equal(
            216m,
            result.TotalAmount);

        OrderItemResponse responseItem =
            Assert.Single(result.Items);

        Assert.Equal(
            orderItem.OrderItemId,
            responseItem.OrderItemId);

        Assert.Equal(
            book.Title,
            responseItem.BookTitle);

        Assert.Equal(
            "Admin Details Store",
            responseItem.SellerStoreName);

        Assert.Equal(
            2,
            responseItem.Quantity);

        Assert.Equal(
            108m,
            responseItem.EffectiveUnitPrice);

        Assert.Equal(
            216m,
            responseItem.LineTotal);
    }

    [Fact]
    public async Task
        GetOrderById_WithMissingOrder_ReturnsNotFound()
    {
        ListingIntegrationTestHelper.TestUserContext
            admin =
                await OrderIntegrationTestHelper
                    .CreateAdminAsync(_factory);

        SetToken(
            admin.AccessToken);

        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/admin/orders/999999");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task
        UpdateOrderItemStatus_FromPendingToConfirmed_UpdatesItemAndOrder()
    {
        ListingIntegrationTestHelper.TestUserContext
            admin =
                await OrderIntegrationTestHelper
                    .CreateAdminAsync(_factory);

        ListingIntegrationTestHelper.TestUserContext
            customer =
                await ListingIntegrationTestHelper
                    .CreateCustomerAsync(_factory);

        ListingIntegrationTestHelper.TestUserContext
            seller =
                await ListingIntegrationTestHelper
                    .CreateSellerAsync(_factory);

        ListingIntegrationTestHelper.TestBookContext
            book =
                await ListingIntegrationTestHelper
                    .CreateBookAsync(_factory);

        ListingIntegrationTestHelper.TestListingContext
            listing =
                await ListingIntegrationTestHelper
                    .CreateListingAsync(
                        _factory,
                        seller.UserId,
                        book.BookId,
                        quantity: 10,
                        status:
                            ListingStatus.Active);

        OrderIntegrationTestHelper.TestOrderContext
            order =
                await OrderIntegrationTestHelper
                    .CreateOrderAsync(
                        _factory,
                        customer.UserId,
                        subtotalAmount: 0m);

        OrderIntegrationTestHelper.TestOrderItemContext
            orderItem =
                await OrderIntegrationTestHelper
                    .CreateOrderItemAsync(
                        _factory,
                        order.OrderId,
                        listing.ListingId);

        SetToken(
            admin.AccessToken);

        var request =
            new UpdateOrderItemStatusRequest
            {
                Status =
                    OrderItemStatus.Confirmed
            };

        HttpResponseMessage response =
            await _client.PutAsJsonAsync(
                "/api/admin/orders/order-items/" +
                $"{orderItem.OrderItemId}/status",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.OK);

        OrderDetailsResponse? result =
            await response.Content
                .ReadFromJsonAsync<
                    OrderDetailsResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            order.OrderId,
            result.OrderId);

        Assert.Equal(
            OrderStatus.Confirmed,
            result.Status);

        OrderItemResponse responseItem =
            Assert.Single(result.Items);

        Assert.Equal(
            orderItem.OrderItemId,
            responseItem.OrderItemId);

        Assert.Equal(
            OrderItemStatus.Confirmed,
            responseItem.Status);

        OrderItem storedItem =
            await OrderIntegrationTestHelper
                .GetOrderItemAsync(
                    _factory,
                    orderItem.OrderItemId);

        Assert.Equal(
            OrderItemStatus.Confirmed,
            storedItem.Status);

        Order storedOrder =
            await OrderIntegrationTestHelper
                .GetOrderAsync(
                    _factory,
                    order.OrderId);

        Assert.Equal(
            OrderStatus.Confirmed,
            storedOrder.Status);
    }

    [Fact]
    public async Task
        UpdateOrderItemStatus_FromPendingToCancelled_RestoresStockAndCancelsOrder()
    {
        ListingIntegrationTestHelper.TestUserContext
            admin =
                await OrderIntegrationTestHelper
                    .CreateAdminAsync(_factory);

        ListingIntegrationTestHelper.TestUserContext
            customer =
                await ListingIntegrationTestHelper
                    .CreateCustomerAsync(_factory);

        ListingIntegrationTestHelper.TestUserContext
            seller =
                await ListingIntegrationTestHelper
                    .CreateSellerAsync(_factory);

        ListingIntegrationTestHelper.TestBookContext
            book =
                await ListingIntegrationTestHelper
                    .CreateBookAsync(_factory);

        ListingIntegrationTestHelper.TestListingContext
            listing =
                await ListingIntegrationTestHelper
                    .CreateListingAsync(
                        _factory,
                        seller.UserId,
                        book.BookId,
                        quantity: 3,
                        status:
                            ListingStatus.Active);

        OrderIntegrationTestHelper.TestOrderContext
            order =
                await OrderIntegrationTestHelper
                    .CreateOrderAsync(
                        _factory,
                        customer.UserId,
                        subtotalAmount: 0m);

        OrderIntegrationTestHelper.TestOrderItemContext
            orderItem =
                await OrderIntegrationTestHelper
                    .CreateOrderItemAsync(
                        _factory,
                        order.OrderId,
                        listing.ListingId,
                        quantity: 2);

        SetToken(
            admin.AccessToken);

        var request =
            new UpdateOrderItemStatusRequest
            {
                Status =
                    OrderItemStatus.Cancelled
            };

        HttpResponseMessage response =
            await _client.PutAsJsonAsync(
                "/api/admin/orders/order-items/" +
                $"{orderItem.OrderItemId}/status",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.OK);

        OrderDetailsResponse? result =
            await response.Content
                .ReadFromJsonAsync<
                    OrderDetailsResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            OrderStatus.Cancelled,
            result.Status);

        OrderItemResponse responseItem =
            Assert.Single(result.Items);

        Assert.Equal(
            OrderItemStatus.Cancelled,
            responseItem.Status);

        Order storedOrder =
            await OrderIntegrationTestHelper
                .GetOrderAsync(
                    _factory,
                    order.OrderId);

        Assert.Equal(
            OrderStatus.Cancelled,
            storedOrder.Status);

        OrderItem storedItem =
            await OrderIntegrationTestHelper
                .GetOrderItemAsync(
                    _factory,
                    orderItem.OrderItemId);

        Assert.Equal(
            OrderItemStatus.Cancelled,
            storedItem.Status);

        Listing storedListing =
            await ListingIntegrationTestHelper
                .GetListingIgnoringFiltersAsync(
                    _factory,
                    listing.ListingId);

        Assert.Equal(
            5,
            storedListing.Quantity);
    }

    [Fact]
    public async Task
        UpdateOrderItemStatus_WithInvalidTransition_ReturnsConflictAndKeepsData()
    {
        ListingIntegrationTestHelper.TestUserContext
            admin =
                await OrderIntegrationTestHelper
                    .CreateAdminAsync(_factory);

        ListingIntegrationTestHelper.TestUserContext
            customer =
                await ListingIntegrationTestHelper
                    .CreateCustomerAsync(_factory);

        ListingIntegrationTestHelper.TestUserContext
            seller =
                await ListingIntegrationTestHelper
                    .CreateSellerAsync(_factory);

        ListingIntegrationTestHelper.TestBookContext
            book =
                await ListingIntegrationTestHelper
                    .CreateBookAsync(_factory);

        ListingIntegrationTestHelper.TestListingContext
            listing =
                await ListingIntegrationTestHelper
                    .CreateListingAsync(
                        _factory,
                        seller.UserId,
                        book.BookId,
                        quantity: 6,
                        status:
                            ListingStatus.Active);

        OrderIntegrationTestHelper.TestOrderContext
            order =
                await OrderIntegrationTestHelper
                    .CreateOrderAsync(
                        _factory,
                        customer.UserId,
                        subtotalAmount: 0m);

        OrderIntegrationTestHelper.TestOrderItemContext
            orderItem =
                await OrderIntegrationTestHelper
                    .CreateOrderItemAsync(
                        _factory,
                        order.OrderId,
                        listing.ListingId,
                        quantity: 2);

        SetToken(
            admin.AccessToken);

        var request =
            new UpdateOrderItemStatusRequest
            {
                Status =
                    OrderItemStatus.Shipped
            };

        HttpResponseMessage response =
            await _client.PutAsJsonAsync(
                "/api/admin/orders/order-items/" +
                $"{orderItem.OrderItemId}/status",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.Conflict);

        OrderItem storedItem =
            await OrderIntegrationTestHelper
                .GetOrderItemAsync(
                    _factory,
                    orderItem.OrderItemId);

        Assert.Equal(
            OrderItemStatus.Pending,
            storedItem.Status);

        Order storedOrder =
            await OrderIntegrationTestHelper
                .GetOrderAsync(
                    _factory,
                    order.OrderId);

        Assert.Equal(
            OrderStatus.Pending,
            storedOrder.Status);

        Listing storedListing =
            await ListingIntegrationTestHelper
                .GetListingIgnoringFiltersAsync(
                    _factory,
                    listing.ListingId);

        Assert.Equal(
            6,
            storedListing.Quantity);
    }

    private void SetToken(
        string accessToken)
    {
        IntegrationTestAuthenticationHelper
            .SetBearerToken(
                _client,
                accessToken);
    }

    private static async Task
        AssertStatusCodeAsync(
            HttpResponseMessage response,
            HttpStatusCode expectedStatusCode)
    {
        if (response.StatusCode ==
            expectedStatusCode)
        {
            return;
        }

        string responseBody =
            await response.Content
                .ReadAsStringAsync();

        Assert.Fail(
            $"Expected HTTP " +
            $"{(int)expectedStatusCode} " +
            $"{expectedStatusCode}, but received " +
            $"{(int)response.StatusCode} " +
            $"{response.StatusCode}." +
            Environment.NewLine +
            $"Response body:" +
            Environment.NewLine +
            responseBody);
    }
}
