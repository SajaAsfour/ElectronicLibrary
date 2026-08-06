using ElectronicLibrary.DAL.DTOs.Requests.Orders;
using ElectronicLibrary.DAL.DTOs.Responses.Orders;
using ElectronicLibrary.DAL.Enums;
using ElectronicLibrary.DAL.Models.Marketplace;
using ElectronicLibrary.DAL.Models.Orders;
using ElectronicLibrary.DAL.DTOs.Responses.Common;

using ElectronicLibrary.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace ElectronicLibrary.IntegrationTests.Controllers;

[Collection(IntegrationTestCollection.Name)]
public sealed class OrdersControllerTests
{
    private readonly CustomWebApplicationFactory
        _factory;

    private readonly HttpClient _client;

    public OrdersControllerTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task
        Checkout_WithoutToken_ReturnsUnauthorizedAndDoesNotChangeData()
    {
        OrderIntegrationTestHelper.TestCheckoutContext
            checkoutContext =
                await OrderIntegrationTestHelper
                    .CreateCheckoutContextAsync(
                        _factory,
                        stockQuantity: 5,
                        cartQuantity: 2);

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        var request =
            new CheckoutRequest();

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/orders/checkout",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.Unauthorized);

        Assert.Equal(
            0,
            await OrderIntegrationTestHelper
                .GetOrderCountForUserAsync(
                    _factory,
                    checkoutContext
                        .Customer.UserId));

        Assert.Equal(
            1,
            await CartIntegrationTestHelper
                .GetCartItemCountForUserAsync(
                    _factory,
                    checkoutContext
                        .Customer.UserId));

        Listing storedListing =
            await ListingIntegrationTestHelper
                .GetListingIgnoringFiltersAsync(
                    _factory,
                    checkoutContext
                        .Listing.ListingId);

        Assert.Equal(
            5,
            storedListing.Quantity);
    }

    [Fact]
    public async Task
        Checkout_WithValidCartWithoutCoupon_CreatesOrderClearsCartAndReducesStock()
    {
        OrderIntegrationTestHelper.TestCheckoutContext
            checkoutContext =
                await OrderIntegrationTestHelper
                    .CreateCheckoutContextAsync(
                        _factory,
                        price: 100m,
                        stockQuantity: 5,
                        cartQuantity: 2,
                        discountPercentage: 10m);

        SetToken(
            checkoutContext.Customer.AccessToken);

        var request =
            new CheckoutRequest();

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/orders/checkout",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.Created);

        OrderDetailsResponse? result =
            await response.Content
                .ReadFromJsonAsync<
                    OrderDetailsResponse>();

        Assert.NotNull(result);

        Assert.True(
            result.OrderId > 0);

        Assert.Equal(
            checkoutContext.Customer.UserId,
            result.UserId);

        Assert.Equal(
            OrderStatus.Pending,
            result.Status);

        Assert.Equal(
            2,
            result.TotalItems);

        Assert.Equal(
            200m,
            result.SubtotalAmount);

        Assert.Equal(
            20m,
            result.ListingDiscountAmount);

        Assert.Equal(
            0m,
            result.CouponDiscountAmount);

        Assert.Equal(
            20m,
            result.TotalDiscountAmount);

        Assert.Equal(
            180m,
            result.TotalAmount);

        Assert.Null(
            result.CouponCode);

        Assert.Null(
            result.CouponDiscountType);

        Assert.Null(
            result.CouponDiscountValue);

        OrderItemResponse responseItem =
            Assert.Single(result.Items);

        Assert.True(
            responseItem.OrderItemId > 0);

        Assert.Equal(
            checkoutContext.Listing.ListingId,
            responseItem.ListingId);

        Assert.Equal(
            checkoutContext.Book.BookId,
            responseItem.BookId);

        Assert.Equal(
            checkoutContext.Seller.UserId,
            responseItem.SellerId);

        Assert.Equal(
            checkoutContext.Book.Title,
            responseItem.BookTitle);

        Assert.Equal(
            checkoutContext.Seller.StoreName,
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

        Assert.NotNull(
            response.Headers.Location);

        Assert.EndsWith(
            $"/api/orders/{result.OrderId}",
            response.Headers.Location.ToString());

        Order storedOrder =
            await OrderIntegrationTestHelper
                .GetOrderAsync(
                    _factory,
                    result.OrderId);

        Assert.Equal(
            checkoutContext.Customer.UserId,
            storedOrder.UserId);

        Assert.Equal(
            OrderStatus.Pending,
            storedOrder.Status);

        Assert.Equal(
            200m,
            storedOrder.SubtotalAmount);

        Assert.Equal(
            20m,
            storedOrder.ListingDiscountAmount);

        Assert.Equal(
            180m,
            storedOrder.TotalAmount);

        OrderItem storedOrderItem =
            Assert.Single(
                storedOrder.OrderItems);

        Assert.Equal(
            checkoutContext.Book.Title,
            storedOrderItem.BookTitleSnapshot);

        Assert.Equal(
            checkoutContext.Seller.StoreName,
            storedOrderItem
                .SellerStoreNameSnapshot);

        Assert.Equal(
            0,
            await CartIntegrationTestHelper
                .GetCartItemCountForUserAsync(
                    _factory,
                    checkoutContext
                        .Customer.UserId));

        Listing storedListing =
            await ListingIntegrationTestHelper
                .GetListingIgnoringFiltersAsync(
                    _factory,
                    checkoutContext
                        .Listing.ListingId);

        Assert.Equal(
            3,
            storedListing.Quantity);

        Assert.Equal(
            ListingStatus.Active,
            storedListing.Status);
    }

    [Fact]
    public async Task
        Checkout_WithPercentageCoupon_AppliesCouponAfterListingDiscount()
    {
        OrderIntegrationTestHelper.TestCheckoutContext
            checkoutContext =
                await OrderIntegrationTestHelper
                    .CreateCheckoutContextAsync(
                        _factory,
                        price: 80m,
                        stockQuantity: 10,
                        cartQuantity: 2,
                        discountPercentage: 25m);

        OrderIntegrationTestHelper.TestCouponContext
            coupon =
                await OrderIntegrationTestHelper
                    .CreateCouponAsync(
                        _factory,
                        code: "READ10",
                        discountValue: 10m,
                        discountType:
                            "Percentage");

        SetToken(
            checkoutContext.Customer.AccessToken);

        var request =
            new CheckoutRequest
            {
                CouponCode =
                    coupon.Code
            };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/orders/checkout",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.Created);

        OrderDetailsResponse? result =
            await response.Content
                .ReadFromJsonAsync<
                    OrderDetailsResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            160m,
            result.SubtotalAmount);

        Assert.Equal(
            40m,
            result.ListingDiscountAmount);

        Assert.Equal(
            12m,
            result.CouponDiscountAmount);

        Assert.Equal(
            52m,
            result.TotalDiscountAmount);

        Assert.Equal(
            108m,
            result.TotalAmount);

        Assert.Equal(
            coupon.Code,
            result.CouponCode);

        Assert.Equal(
            "Percentage",
            result.CouponDiscountType);

        Assert.Equal(
            10m,
            result.CouponDiscountValue);

        Order storedOrder =
            await OrderIntegrationTestHelper
                .GetOrderAsync(
                    _factory,
                    result.OrderId);

        Assert.Equal(
            coupon.CouponId,
            storedOrder.CouponId);

        Assert.Equal(
            coupon.Code,
            storedOrder.CouponCodeSnapshot);

        Assert.Equal(
            "Percentage",
            storedOrder
                .CouponDiscountTypeSnapshot);

        Assert.Equal(
            10m,
            storedOrder
                .CouponDiscountValueSnapshot);

        Assert.Equal(
            12m,
            storedOrder.CouponDiscountAmount);

        Assert.Equal(
            108m,
            storedOrder.TotalAmount);

        Assert.Equal(
            0,
            await CartIntegrationTestHelper
                .GetCartItemCountForUserAsync(
                    _factory,
                    checkoutContext
                        .Customer.UserId));

        Listing storedListing =
            await ListingIntegrationTestHelper
                .GetListingIgnoringFiltersAsync(
                    _factory,
                    checkoutContext
                        .Listing.ListingId);

        Assert.Equal(
            8,
            storedListing.Quantity);
    }

    [Fact]
    public async Task
        Checkout_WithEmptyCart_ReturnsBadRequestWithoutCreatingOrder()
    {
        ListingIntegrationTestHelper.TestUserContext
            customer =
                await ListingIntegrationTestHelper
                    .CreateCustomerAsync(_factory);

        await CartIntegrationTestHelper
            .CreateCartAsync(
                _factory,
                customer.UserId);

        SetToken(
            customer.AccessToken);

        var request =
            new CheckoutRequest();

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/orders/checkout",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.BadRequest);

        Assert.Equal(
            0,
            await OrderIntegrationTestHelper
                .GetOrderCountForUserAsync(
                    _factory,
                    customer.UserId));

        Assert.Equal(
            0,
            await CartIntegrationTestHelper
                .GetCartItemCountForUserAsync(
                    _factory,
                    customer.UserId));
    }

    [Fact]
    public async Task
        Checkout_WhenQuantityExceedsStock_ReturnsBadRequestAndKeepsCartAndStock()
    {
        OrderIntegrationTestHelper.TestCheckoutContext
            checkoutContext =
                await OrderIntegrationTestHelper
                    .CreateCheckoutContextAsync(
                        _factory,
                        stockQuantity: 2,
                        cartQuantity: 3);

        SetToken(
            checkoutContext.Customer.AccessToken);

        var request =
            new CheckoutRequest();

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/orders/checkout",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.BadRequest);

        Assert.Equal(
            0,
            await OrderIntegrationTestHelper
                .GetOrderCountForUserAsync(
                    _factory,
                    checkoutContext
                        .Customer.UserId));

        Assert.Equal(
            1,
            await CartIntegrationTestHelper
                .GetCartItemCountForUserAsync(
                    _factory,
                    checkoutContext
                        .Customer.UserId));

        Listing storedListing =
            await ListingIntegrationTestHelper
                .GetListingIgnoringFiltersAsync(
                    _factory,
                    checkoutContext
                        .Listing.ListingId);

        Assert.Equal(
            2,
            storedListing.Quantity);

        Assert.Equal(
            ListingStatus.Active,
            storedListing.Status);
    }

    [Fact]
    public async Task
    GetCurrentUserOrders_WithoutToken_ReturnsUnauthorized()
    {
        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/orders");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task
        GetCurrentUserOrders_ReturnsOnlyCurrentUsersOrders()
    {
        ListingIntegrationTestHelper.TestUserContext
            customer =
                await ListingIntegrationTestHelper
                    .CreateCustomerAsync(_factory);

        ListingIntegrationTestHelper.TestUserContext
            anotherCustomer =
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
                        quantity: 10,
                        status:
                            ListingStatus.Active);

        OrderIntegrationTestHelper.TestOrderContext
            olderOrder =
                await OrderIntegrationTestHelper
                    .CreateOrderAsync(
                        _factory,
                        customer.UserId,
                        subtotalAmount: 0m,
                        orderDate:
                            DateTime.UtcNow.AddDays(-2));

        await OrderIntegrationTestHelper
            .CreateOrderItemAsync(
                _factory,
                olderOrder.OrderId,
                listing.ListingId,
                quantity: 1);

        OrderIntegrationTestHelper.TestOrderContext
            newerOrder =
                await OrderIntegrationTestHelper
                    .CreateOrderAsync(
                        _factory,
                        customer.UserId,
                        subtotalAmount: 0m,
                        orderDate:
                            DateTime.UtcNow.AddDays(-1));

        await OrderIntegrationTestHelper
            .CreateOrderItemAsync(
                _factory,
                newerOrder.OrderId,
                listing.ListingId,
                quantity: 2);

        OrderIntegrationTestHelper.TestOrderContext
            anotherUsersOrder =
                await OrderIntegrationTestHelper
                    .CreateOrderAsync(
                        _factory,
                        anotherCustomer.UserId,
                        subtotalAmount: 0m);

        await OrderIntegrationTestHelper
            .CreateOrderItemAsync(
                _factory,
                anotherUsersOrder.OrderId,
                listing.ListingId,
                quantity: 3);

        SetToken(
            customer.AccessToken);

        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/orders" +
                "?sortBy=orderDate" +
                "&sortDirection=desc" +
                "&pageNumber=1" +
                "&pageSize=10");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.OK);

        PagedResponse<OrderSummaryResponse>? result =
            await response.Content
                .ReadFromJsonAsync<
                    PagedResponse<
                        OrderSummaryResponse>>();

        Assert.NotNull(result);

        Assert.Equal(
            2,
            result.TotalCount);

        Assert.Equal(
            1,
            result.PageNumber);

        Assert.Equal(
            10,
            result.PageSize);

        Assert.Equal(
            2,
            result.Items.Count);

        List<OrderSummaryResponse> orders =
            result.Items.ToList();

                Assert.Equal(
                    newerOrder.OrderId,
                    orders[0].OrderId);

                Assert.Equal(
                    olderOrder.OrderId,

            orders[1].OrderId);

        Assert.DoesNotContain(
            result.Items,
            order =>
                order.OrderId ==
                anotherUsersOrder.OrderId);

        Assert.All(
            result.Items,
            order =>
            {
                Assert.Equal(
                    OrderStatus.Pending,
                    order.Status);

                Assert.True(
                    order.TotalItems > 0);
            });
    }

    [Fact]
    public async Task
        GetCurrentUserOrderById_WithOwnedOrder_ReturnsOrderDetails()
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
                        "Order Details Store");

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
                        discountPercentage: 25m,
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
            customer.AccessToken);

        HttpResponseMessage response =
            await _client.GetAsync(
                $"/api/orders/{order.OrderId}");

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
            60m,
            result.ListingDiscountAmount);

        Assert.Equal(
            180m,
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
            "Order Details Store",
            responseItem.SellerStoreName);

        Assert.Equal(
            2,
            responseItem.Quantity);

        Assert.Equal(
            120m,
            responseItem.UnitPrice);

        Assert.Equal(
            90m,
            responseItem.EffectiveUnitPrice);

        Assert.Equal(
            180m,
            responseItem.LineTotal);
    }

    [Fact]
    public async Task
        GetCurrentUserOrderById_WithAnotherUsersOrder_ReturnsNotFound()
    {
        ListingIntegrationTestHelper.TestUserContext
            owner =
                await ListingIntegrationTestHelper
                    .CreateCustomerAsync(_factory);

        ListingIntegrationTestHelper.TestUserContext
            anotherCustomer =
                await ListingIntegrationTestHelper
                    .CreateCustomerAsync(_factory);

        OrderIntegrationTestHelper.TestOrderContext
            order =
                await OrderIntegrationTestHelper
                    .CreateOrderAsync(
                        _factory,
                        owner.UserId);

        SetToken(
            anotherCustomer.AccessToken);

        HttpResponseMessage response =
            await _client.GetAsync(
                $"/api/orders/{order.OrderId}");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task
    CancelCurrentUserOrder_WithoutToken_ReturnsUnauthorizedAndKeepsOrder()
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

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        HttpResponseMessage response =
            await _client.PostAsync(
                $"/api/orders/{order.OrderId}/cancel",
                content: null);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.Unauthorized);

        Order storedOrder =
            await OrderIntegrationTestHelper
                .GetOrderAsync(
                    _factory,
                    order.OrderId);

        Assert.Equal(
            OrderStatus.Pending,
            storedOrder.Status);

        OrderItem storedItem =
            await OrderIntegrationTestHelper
                .GetOrderItemAsync(
                    _factory,
                    orderItem.OrderItemId);

        Assert.Equal(
            OrderItemStatus.Pending,
            storedItem.Status);

        Listing storedListing =
            await ListingIntegrationTestHelper
                .GetListingIgnoringFiltersAsync(
                    _factory,
                    listing.ListingId);

        Assert.Equal(
            3,
            storedListing.Quantity);
    }

    [Fact]
    public async Task
        CancelCurrentUserOrder_WithPendingOrder_CancelsOrderAndRestoresStock()
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
                        "Cancellation Store");

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
                        price: 70m,
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
            customer.AccessToken);

        HttpResponseMessage response =
            await _client.PostAsync(
                $"/api/orders/{order.OrderId}/cancel",
                content: null);

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
            OrderStatus.Cancelled,
            result.Status);

        OrderItemResponse responseItem =
            Assert.Single(result.Items);

        Assert.Equal(
            orderItem.OrderItemId,
            responseItem.OrderItemId);

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
        CancelCurrentUserOrder_WithAnotherUsersOrder_ReturnsNotFound()
    {
        ListingIntegrationTestHelper.TestUserContext
            owner =
                await ListingIntegrationTestHelper
                    .CreateCustomerAsync(_factory);

        ListingIntegrationTestHelper.TestUserContext
            anotherCustomer =
                await ListingIntegrationTestHelper
                    .CreateCustomerAsync(_factory);

        OrderIntegrationTestHelper.TestOrderContext
            order =
                await OrderIntegrationTestHelper
                    .CreateOrderAsync(
                        _factory,
                        owner.UserId);

        SetToken(
            anotherCustomer.AccessToken);

        HttpResponseMessage response =
            await _client.PostAsync(
                $"/api/orders/{order.OrderId}/cancel",
                content: null);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.NotFound);

        Order storedOrder =
            await OrderIntegrationTestHelper
                .GetOrderAsync(
                    _factory,
                    order.OrderId);

        Assert.Equal(
            OrderStatus.Pending,
            storedOrder.Status);
    }

    [Fact]
    public async Task
        CancelCurrentUserOrder_WithProcessingOrder_ReturnsConflictAndKeepsData()
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
                        quantity: 4,
                        status:
                            ListingStatus.Active);

        OrderIntegrationTestHelper.TestOrderContext
            order =
                await OrderIntegrationTestHelper
                    .CreateOrderAsync(
                        _factory,
                        customer.UserId,
                        status:
                            OrderStatus.Processing,
                        subtotalAmount: 0m);

        OrderIntegrationTestHelper.TestOrderItemContext
            orderItem =
                await OrderIntegrationTestHelper
                    .CreateOrderItemAsync(
                        _factory,
                        order.OrderId,
                        listing.ListingId,
                        quantity: 2,
                        status:
                            OrderItemStatus.Processing);

        SetToken(
            customer.AccessToken);

        HttpResponseMessage response =
            await _client.PostAsync(
                $"/api/orders/{order.OrderId}/cancel",
                content: null);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.Conflict);

        Order storedOrder =
            await OrderIntegrationTestHelper
                .GetOrderAsync(
                    _factory,
                    order.OrderId);

        Assert.Equal(
            OrderStatus.Processing,
            storedOrder.Status);

        OrderItem storedItem =
            await OrderIntegrationTestHelper
                .GetOrderItemAsync(
                    _factory,
                    orderItem.OrderItemId);

        Assert.Equal(
            OrderItemStatus.Processing,
            storedItem.Status);

        Listing storedListing =
            await ListingIntegrationTestHelper
                .GetListingIgnoringFiltersAsync(
                    _factory,
                    listing.ListingId);

        Assert.Equal(
            4,
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