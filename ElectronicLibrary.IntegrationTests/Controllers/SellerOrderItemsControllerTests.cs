using ElectronicLibrary.DAL.DTOs.Requests.Orders;
using ElectronicLibrary.DAL.DTOs.Responses.Common;
using ElectronicLibrary.DAL.DTOs.Responses.Orders;
using ElectronicLibrary.DAL.Enums;
using ElectronicLibrary.DAL.Models.Orders;
using ElectronicLibrary.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace ElectronicLibrary.IntegrationTests.Controllers;

[Collection(IntegrationTestCollection.Name)]
public sealed class SellerOrderItemsControllerTests
{
    private readonly CustomWebApplicationFactory
        _factory;

    private readonly HttpClient _client;

    public SellerOrderItemsControllerTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task
        GetCurrentSellerOrderItems_WithoutToken_ReturnsUnauthorized()
    {
        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/seller/order-items");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task
        GetCurrentSellerOrderItems_WithCustomerToken_ReturnsForbidden()
    {
        ListingIntegrationTestHelper.TestUserContext
            customer =
                await ListingIntegrationTestHelper
                    .CreateCustomerAsync(_factory);

        SetToken(
            customer.AccessToken);

        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/seller/order-items");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task
        GetCurrentSellerOrderItems_ReturnsOnlyCurrentSellersItems()
    {
        ListingIntegrationTestHelper.TestUserContext
            customer =
                await ListingIntegrationTestHelper
                    .CreateCustomerAsync(_factory);

        ListingIntegrationTestHelper.TestUserContext
            seller =
                await ListingIntegrationTestHelper
                    .CreateSellerAsync(
                        _factory,
                        "Primary Seller Store");

        ListingIntegrationTestHelper.TestUserContext
            anotherSeller =
                await ListingIntegrationTestHelper
                    .CreateSellerAsync(
                        _factory,
                        "Another Seller Store");

        ListingIntegrationTestHelper.TestBookContext
            firstBook =
                await ListingIntegrationTestHelper
                    .CreateBookAsync(_factory);

        ListingIntegrationTestHelper.TestBookContext
            secondBook =
                await ListingIntegrationTestHelper
                    .CreateBookAsync(_factory);

        ListingIntegrationTestHelper.TestListingContext
            sellerListing =
                await ListingIntegrationTestHelper
                    .CreateListingAsync(
                        _factory,
                        seller.UserId,
                        firstBook.BookId,
                        price: 60m,
                        quantity: 10,
                        status:
                            ListingStatus.Active);

        ListingIntegrationTestHelper.TestListingContext
            anotherSellerListing =
                await ListingIntegrationTestHelper
                    .CreateListingAsync(
                        _factory,
                        anotherSeller.UserId,
                        secondBook.BookId,
                        price: 90m,
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
            sellerOrderItem =
                await OrderIntegrationTestHelper
                    .CreateOrderItemAsync(
                        _factory,
                        order.OrderId,
                        sellerListing.ListingId,
                        quantity: 2);

        OrderIntegrationTestHelper.TestOrderItemContext
            anotherSellerOrderItem =
                await OrderIntegrationTestHelper
                    .CreateOrderItemAsync(
                        _factory,
                        order.OrderId,
                        anotherSellerListing.ListingId,
                        quantity: 1);

        SetToken(
            seller.AccessToken);

        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/seller/order-items" +
                "?pageNumber=1" +
                "&pageSize=10");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.OK);

        PagedResponse<SellerOrderItemResponse>?
            result =
                await response.Content
                    .ReadFromJsonAsync<
                        PagedResponse<
                            SellerOrderItemResponse>>();

        Assert.NotNull(result);

        Assert.Equal(
            1,
            result.TotalCount);

        SellerOrderItemResponse responseItem =
            Assert.Single(result.Items);

        Assert.Equal(
            sellerOrderItem.OrderItemId,
            responseItem.OrderItemId);

        Assert.Equal(
            order.OrderId,
            responseItem.OrderId);

        Assert.Equal(
            seller.UserId,
            responseItem.SellerId);

        Assert.Equal(
            "Primary Seller Store",
            responseItem.SellerStoreName);

        Assert.Equal(
            firstBook.BookId,
            responseItem.BookId);

        Assert.Equal(
            2,
            responseItem.Quantity);

        Assert.Equal(
            OrderItemStatus.Pending,
            responseItem.Status);

        Assert.DoesNotContain(
            result.Items,
            item =>
                item.OrderItemId ==
                anotherSellerOrderItem.OrderItemId);
    }

    [Fact]
    public async Task
        GetCurrentSellerOrderItems_WithOrderAndStatusFilters_ReturnsMatchingItems()
    {
        ListingIntegrationTestHelper.TestUserContext
            customer =
                await ListingIntegrationTestHelper
                    .CreateCustomerAsync(_factory);

        ListingIntegrationTestHelper.TestUserContext
            seller =
                await ListingIntegrationTestHelper
                    .CreateSellerAsync(_factory);

        ListingIntegrationTestHelper.TestBookContext
            firstBook =
                await ListingIntegrationTestHelper
                    .CreateBookAsync(_factory);

        ListingIntegrationTestHelper.TestBookContext
            secondBook =
                await ListingIntegrationTestHelper
                    .CreateBookAsync(_factory);

        ListingIntegrationTestHelper.TestListingContext
            firstListing =
                await ListingIntegrationTestHelper
                    .CreateListingAsync(
                        _factory,
                        seller.UserId,
                        firstBook.BookId,
                        quantity: 10,
                        status:
                            ListingStatus.Active);

        ListingIntegrationTestHelper.TestListingContext
            secondListing =
                await ListingIntegrationTestHelper
                    .CreateListingAsync(
                        _factory,
                        seller.UserId,
                        secondBook.BookId,
                        quantity: 10,
                        status:
                            ListingStatus.Active);

        OrderIntegrationTestHelper.TestOrderContext
            firstOrder =
                await OrderIntegrationTestHelper
                    .CreateOrderAsync(
                        _factory,
                        customer.UserId,
                        subtotalAmount: 0m);

        OrderIntegrationTestHelper.TestOrderItemContext
            matchingItem =
                await OrderIntegrationTestHelper
                    .CreateOrderItemAsync(
                        _factory,
                        firstOrder.OrderId,
                        firstListing.ListingId,
                        status:
                            OrderItemStatus.Confirmed);

        await OrderIntegrationTestHelper
            .CreateOrderItemAsync(
                _factory,
                firstOrder.OrderId,
                secondListing.ListingId,
                status:
                    OrderItemStatus.Pending);

        OrderIntegrationTestHelper.TestOrderContext
            secondOrder =
                await OrderIntegrationTestHelper
                    .CreateOrderAsync(
                        _factory,
                        customer.UserId,
                        subtotalAmount: 0m);

        await OrderIntegrationTestHelper
            .CreateOrderItemAsync(
                _factory,
                secondOrder.OrderId,
                firstListing.ListingId,
                status:
                    OrderItemStatus.Confirmed);

        SetToken(
            seller.AccessToken);

        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/seller/order-items" +
                $"?orderId={firstOrder.OrderId}" +
                $"&status={(int)OrderItemStatus.Confirmed}" +
                "&pageNumber=1" +
                "&pageSize=10");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.OK);

        PagedResponse<SellerOrderItemResponse>?
            result =
                await response.Content
                    .ReadFromJsonAsync<
                        PagedResponse<
                            SellerOrderItemResponse>>();

        Assert.NotNull(result);

        Assert.Equal(
            1,
            result.TotalCount);

        SellerOrderItemResponse responseItem =
            Assert.Single(result.Items);

        Assert.Equal(
            matchingItem.OrderItemId,
            responseItem.OrderItemId);

        Assert.Equal(
            firstOrder.OrderId,
            responseItem.OrderId);

        Assert.Equal(
            OrderItemStatus.Confirmed,
            responseItem.Status);
    }

    [Fact]
    public async Task
        UpdateCurrentSellerOrderItemStatus_FromPendingToConfirmed_UpdatesItemAndOrder()
    {
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
            seller.AccessToken);

        var request =
            new UpdateOrderItemStatusRequest
            {
                Status =
                    OrderItemStatus.Confirmed
            };

        HttpResponseMessage response =
            await _client.PutAsJsonAsync(
                $"/api/seller/order-items/" +
                $"{orderItem.OrderItemId}/status",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.OK);

        SellerOrderItemResponse? result =
            await response.Content
                .ReadFromJsonAsync<
                    SellerOrderItemResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            orderItem.OrderItemId,
            result.OrderItemId);

        Assert.Equal(
            OrderItemStatus.Confirmed,
            result.Status);

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
        UpdateCurrentSellerOrderItemStatus_ForAnotherSellersItem_ReturnsNotFound()
    {
        ListingIntegrationTestHelper.TestUserContext
            customer =
                await ListingIntegrationTestHelper
                    .CreateCustomerAsync(_factory);

        ListingIntegrationTestHelper.TestUserContext
            ownerSeller =
                await ListingIntegrationTestHelper
                    .CreateSellerAsync(_factory);

        ListingIntegrationTestHelper.TestUserContext
            anotherSeller =
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
                        ownerSeller.UserId,
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
            anotherSeller.AccessToken);

        var request =
            new UpdateOrderItemStatusRequest
            {
                Status =
                    OrderItemStatus.Confirmed
            };

        HttpResponseMessage response =
            await _client.PutAsJsonAsync(
                $"/api/seller/order-items/" +
                $"{orderItem.OrderItemId}/status",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.NotFound);

        OrderItem storedItem =
            await OrderIntegrationTestHelper
                .GetOrderItemAsync(
                    _factory,
                    orderItem.OrderItemId);

        Assert.Equal(
            OrderItemStatus.Pending,
            storedItem.Status);
    }

    [Fact]
    public async Task
        UpdateCurrentSellerOrderItemStatus_WithInvalidTransition_ReturnsConflict()
    {
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
            seller.AccessToken);

        var request =
            new UpdateOrderItemStatusRequest
            {
                Status =
                    OrderItemStatus.Shipped
            };

        HttpResponseMessage response =
            await _client.PutAsJsonAsync(
                $"/api/seller/order-items/" +
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
