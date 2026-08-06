using ElectronicLibrary.DAL.DTOs.Requests.Carts;
using ElectronicLibrary.DAL.DTOs.Responses.Carts;
using ElectronicLibrary.DAL.Models.Shopping;
using ElectronicLibrary.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace ElectronicLibrary.IntegrationTests.Controllers;

[Collection(IntegrationTestCollection.Name)]
public sealed class CartControllerTests
{
    private readonly CustomWebApplicationFactory
        _factory;

    private readonly HttpClient _client;

    public CartControllerTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCart_WithoutToken_ReturnsUnauthorized()
    {
        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/cart");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCart_WhenCustomerHasNoCart_ReturnsEmptyCartAndCreatesIt()
    {
        ListingIntegrationTestHelper.TestUserContext
            customer =
                await ListingIntegrationTestHelper
                    .CreateCustomerAsync(_factory);

        SetToken(customer.AccessToken);

        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/cart");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.OK);

        CartResponse? result =
            await response.Content
                .ReadFromJsonAsync<CartResponse>();

        Assert.NotNull(result);

        Assert.True(result.CartId > 0);

        Assert.Equal(
            customer.UserId,
            result.UserId);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalItems);
        Assert.Equal(0m, result.Subtotal);
        Assert.Equal(0m, result.TotalDiscount);
        Assert.Equal(0m, result.FinalTotal);

        Assert.Equal(
            1,
            await CartIntegrationTestHelper
                .GetCartCountForUserAsync(
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
    public async Task GetCart_WhenCartAlreadyExists_ReturnsSameCartWithoutDuplicate()
    {
        ListingIntegrationTestHelper.TestUserContext
            customer =
                await ListingIntegrationTestHelper
                    .CreateCustomerAsync(_factory);

        CartIntegrationTestHelper.TestCartContext
            cart =
                await CartIntegrationTestHelper
                    .CreateCartAsync(
                        _factory,
                        customer.UserId);

        SetToken(customer.AccessToken);

        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/cart");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.OK);

        CartResponse? result =
            await response.Content
                .ReadFromJsonAsync<CartResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            cart.CartId,
            result.CartId);

        Assert.Equal(
            customer.UserId,
            result.UserId);

        Assert.Empty(result.Items);

        Assert.Equal(
            1,
            await CartIntegrationTestHelper
                .GetCartCountForUserAsync(
                    _factory,
                    customer.UserId));
    }

    [Fact]
    public async Task GetCart_WhenCartContainsActiveItem_ReturnsItemAndCalculatedTotals()
    {
        CartIntegrationTestHelper
            .TestCartMarketplaceContext
            marketplace =
                await CartIntegrationTestHelper
                    .CreateMarketplaceContextAsync(
                        _factory,
                        price: 100m,
                        quantity: 5,
                        discountPercentage: 10m);

        CartIntegrationTestHelper.TestCartContext
            cart =
                await CartIntegrationTestHelper
                    .CreateCartAsync(
                        _factory,
                        marketplace.Customer.UserId);

        await CartIntegrationTestHelper
            .CreateCartItemAsync(
                _factory,
                cart.CartId,
                marketplace.Listing.ListingId,
                quantity: 2);

        SetToken(
            marketplace.Customer.AccessToken);

        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/cart");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.OK);

        CartResponse? result =
            await response.Content
                .ReadFromJsonAsync<CartResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            cart.CartId,
            result.CartId);

        Assert.Equal(
            marketplace.Customer.UserId,
            result.UserId);

        CartItemResponse item =
            Assert.Single(result.Items);

        Assert.Equal(
            marketplace.Listing.ListingId,
            item.ListingId);

        Assert.Equal(
            marketplace.Book.BookId,
            item.BookId);

        Assert.Equal(
            marketplace.Book.Title,
            item.BookTitle);

        Assert.Equal(
            marketplace.Seller.UserId,
            item.SellerId);

        Assert.Equal(
            marketplace.Seller.StoreName,
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
        Assert.Null(item.AvailabilityMessage);

        Assert.Equal(2, result.TotalItems);
        Assert.Equal(200m, result.Subtotal);
        Assert.Equal(20m, result.TotalDiscount);
        Assert.Equal(180m, result.FinalTotal);
    }

    [Fact]
    public async Task GetCart_ReturnsOnlyCurrentUsersItems()
    {
        CartIntegrationTestHelper
            .TestCartMarketplaceContext
            marketplace =
                await CartIntegrationTestHelper
                    .CreateMarketplaceContextAsync(
                        _factory,
                        quantity: 10);

        ListingIntegrationTestHelper.TestUserContext
            anotherCustomer =
                await ListingIntegrationTestHelper
                    .CreateCustomerAsync(_factory);

        CartIntegrationTestHelper.TestCartContext
            currentCart =
                await CartIntegrationTestHelper
                    .CreateCartAsync(
                        _factory,
                        marketplace.Customer.UserId);

        CartIntegrationTestHelper.TestCartContext
            anotherCart =
                await CartIntegrationTestHelper
                    .CreateCartAsync(
                        _factory,
                        anotherCustomer.UserId);

        await CartIntegrationTestHelper
            .CreateCartItemAsync(
                _factory,
                currentCart.CartId,
                marketplace.Listing.ListingId,
                quantity: 2);

        await CartIntegrationTestHelper
            .CreateCartItemAsync(
                _factory,
                anotherCart.CartId,
                marketplace.Listing.ListingId,
                quantity: 4);

        SetToken(
            marketplace.Customer.AccessToken);

        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/cart");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.OK);

        CartResponse? result =
            await response.Content
                .ReadFromJsonAsync<CartResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            currentCart.CartId,
            result.CartId);

        CartItemResponse item =
            Assert.Single(result.Items);

        Assert.Equal(2, item.Quantity);
        Assert.Equal(2, result.TotalItems);

        CartItem? anotherStoredItem =
            await CartIntegrationTestHelper
                .GetCartItemAsync(
                    _factory,
                    anotherCart.CartId,
                    marketplace.Listing.ListingId);

        Assert.NotNull(anotherStoredItem);
        Assert.Equal(
            4,
            anotherStoredItem.Quantity);
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
            $"Expected HTTP {(int)expectedStatusCode} " +
            $"{expectedStatusCode}, but received " +
            $"{(int)response.StatusCode} " +
            $"{response.StatusCode}.{Environment.NewLine}" +
            $"Response body:{Environment.NewLine}" +
            responseBody);
    }

    [Fact]
    public async Task AddCartItem_WithoutToken_ReturnsUnauthorized()
    {
        CartIntegrationTestHelper.TestCartMarketplaceContext marketplace =
            await CartIntegrationTestHelper
                .CreateMarketplaceContextAsync(
                    _factory);

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        var request = new AddCartItemRequest
        {
            ListingId =
                marketplace.Listing.ListingId,
            Quantity = 1
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/cart/items",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.Unauthorized);

        Assert.Equal(
            0,
            await CartIntegrationTestHelper
                .GetCartCountForUserAsync(
                    _factory,
                    marketplace.Customer.UserId));
    }

    [Fact]
    public async Task AddCartItem_WithValidRequest_CreatesCartItemAndReturnsTotals()
    {
        CartIntegrationTestHelper.TestCartMarketplaceContext marketplace =
            await CartIntegrationTestHelper
                .CreateMarketplaceContextAsync(
                    _factory,
                    price: 80m,
                    quantity: 10,
                    discountPercentage: 25m);

        SetToken(
            marketplace.Customer.AccessToken);

        var request = new AddCartItemRequest
        {
            ListingId =
                marketplace.Listing.ListingId,
            Quantity = 2
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/cart/items",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.OK);

        CartResponse? result =
            await response.Content
                .ReadFromJsonAsync<CartResponse>();

        Assert.NotNull(result);

        Assert.True(result.CartId > 0);

        Assert.Equal(
            marketplace.Customer.UserId,
            result.UserId);

        CartItemResponse item =
            Assert.Single(result.Items);

        Assert.Equal(
            marketplace.Listing.ListingId,
            item.ListingId);

        Assert.Equal(2, item.Quantity);
        Assert.Equal(10, item.AvailableQuantity);

        Assert.Equal(80m, item.UnitPrice);
        Assert.Equal(
            25m,
            item.DiscountPercentage);
        Assert.Equal(
            60m,
            item.EffectiveUnitPrice);

        Assert.Equal(
            160m,
            item.LineSubtotal);
        Assert.Equal(
            40m,
            item.LineDiscount);
        Assert.Equal(
            120m,
            item.LineTotal);

        Assert.Equal(2, result.TotalItems);
        Assert.Equal(160m, result.Subtotal);
        Assert.Equal(
            40m,
            result.TotalDiscount);
        Assert.Equal(120m, result.FinalTotal);

        Cart? storedCart =
            await CartIntegrationTestHelper
                .GetCartByUserIdAsync(
                    _factory,
                    marketplace.Customer.UserId);

        Assert.NotNull(storedCart);

        CartItem? storedItem =
            await CartIntegrationTestHelper
                .GetCartItemAsync(
                    _factory,
                    storedCart.CartId,
                    marketplace.Listing.ListingId);

        Assert.NotNull(storedItem);
        Assert.Equal(2, storedItem.Quantity);
    }

    [Fact]
    public async Task AddCartItem_WhenItemAlreadyExists_IncreasesExistingQuantity()
    {
        CartIntegrationTestHelper.TestCartMarketplaceContext marketplace =
            await CartIntegrationTestHelper
                .CreateMarketplaceContextAsync(
                    _factory,
                    price: 50m,
                    quantity: 10);

        CartIntegrationTestHelper.TestCartContext cart =
            await CartIntegrationTestHelper
                .CreateCartAsync(
                    _factory,
                    marketplace.Customer.UserId);

        await CartIntegrationTestHelper
            .CreateCartItemAsync(
                _factory,
                cart.CartId,
                marketplace.Listing.ListingId,
                quantity: 2);

        SetToken(
            marketplace.Customer.AccessToken);

        var request = new AddCartItemRequest
        {
            ListingId =
                marketplace.Listing.ListingId,
            Quantity = 3
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/cart/items",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.OK);

        CartResponse? result =
            await response.Content
                .ReadFromJsonAsync<CartResponse>();

        Assert.NotNull(result);

        CartItemResponse item =
            Assert.Single(result.Items);

        Assert.Equal(5, item.Quantity);
        Assert.Equal(5, result.TotalItems);
        Assert.Equal(250m, result.FinalTotal);

        CartItem? storedItem =
            await CartIntegrationTestHelper
                .GetCartItemAsync(
                    _factory,
                    cart.CartId,
                    marketplace.Listing.ListingId);

        Assert.NotNull(storedItem);
        Assert.Equal(5, storedItem.Quantity);

        Assert.Equal(
            1,
            await CartIntegrationTestHelper
                .GetCartItemCountForUserAsync(
                    _factory,
                    marketplace.Customer.UserId));
    }

    [Fact]
    public async Task AddCartItem_WithMissingListing_ReturnsNotFound()
    {
        ListingIntegrationTestHelper.TestUserContext customer =
            await ListingIntegrationTestHelper
                .CreateCustomerAsync(_factory);

        SetToken(customer.AccessToken);

        var request = new AddCartItemRequest
        {
            ListingId = 999999,
            Quantity = 1
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/cart/items",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.NotFound);

        Assert.Equal(
            0,
            await CartIntegrationTestHelper
                .GetCartCountForUserAsync(
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
    public async Task AddCartItem_WithUnavailableListing_ReturnsBadRequest()
    {
        CartIntegrationTestHelper.TestCartMarketplaceContext marketplace =
            await CartIntegrationTestHelper
                .CreateMarketplaceContextAsync(
                    _factory,
                    quantity: 5,
                    status:
                        ElectronicLibrary.DAL.Enums
                            .ListingStatus.Suspended);

        SetToken(
            marketplace.Customer.AccessToken);

        var request = new AddCartItemRequest
        {
            ListingId =
                marketplace.Listing.ListingId,
            Quantity = 1
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/cart/items",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.BadRequest);

        Assert.Equal(
            0,
            await CartIntegrationTestHelper
                .GetCartItemCountForUserAsync(
                    _factory,
                    marketplace.Customer.UserId));
    }

    [Fact]
    public async Task AddCartItem_WhenQuantityExceedsStock_ReturnsBadRequest()
    {
        CartIntegrationTestHelper.TestCartMarketplaceContext marketplace =
            await CartIntegrationTestHelper
                .CreateMarketplaceContextAsync(
                    _factory,
                    quantity: 3);

        SetToken(
            marketplace.Customer.AccessToken);

        var request = new AddCartItemRequest
        {
            ListingId =
                marketplace.Listing.ListingId,
            Quantity = 4
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/cart/items",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.BadRequest);

        Assert.Equal(
            0,
            await CartIntegrationTestHelper
                .GetCartItemCountForUserAsync(
                    _factory,
                    marketplace.Customer.UserId));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public async Task AddCartItem_WithInvalidRequest_ReturnsBadRequest(
        int listingId,
        int quantity)
    {
        ListingIntegrationTestHelper.TestUserContext customer =
            await ListingIntegrationTestHelper
                .CreateCustomerAsync(_factory);

        SetToken(customer.AccessToken);

        var request = new AddCartItemRequest
        {
            ListingId = listingId,
            Quantity = quantity
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/cart/items",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.BadRequest);

        Assert.Equal(
            0,
            await CartIntegrationTestHelper
                .GetCartItemCountForUserAsync(
                    _factory,
                    customer.UserId));
    }

    [Fact]
    public async Task UpdateCartItem_WithoutToken_ReturnsUnauthorizedAndKeepsQuantity()
    {
        CartIntegrationTestHelper.TestCartMarketplaceContext marketplace =
            await CartIntegrationTestHelper
                .CreateMarketplaceContextAsync(
                    _factory,
                    quantity: 10);

        CartIntegrationTestHelper.TestCartContext cart =
            await CartIntegrationTestHelper
                .CreateCartAsync(
                    _factory,
                    marketplace.Customer.UserId);

        await CartIntegrationTestHelper
            .CreateCartItemAsync(
                _factory,
                cart.CartId,
                marketplace.Listing.ListingId,
                quantity: 2);

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        var request =
            new UpdateCartItemQuantityRequest
            {
                Quantity = 4
            };

        HttpResponseMessage response =
            await _client.PutAsJsonAsync(
                $"/api/cart/items/" +
                $"{marketplace.Listing.ListingId}",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.Unauthorized);

        CartItem? storedItem =
            await CartIntegrationTestHelper
                .GetCartItemAsync(
                    _factory,
                    cart.CartId,
                    marketplace.Listing.ListingId);

        Assert.NotNull(storedItem);
        Assert.Equal(2, storedItem.Quantity);
    }

    [Fact]
    public async Task UpdateCartItem_WithValidQuantity_UpdatesItemAndReturnsTotals()
    {
        CartIntegrationTestHelper.TestCartMarketplaceContext marketplace =
            await CartIntegrationTestHelper
                .CreateMarketplaceContextAsync(
                    _factory,
                    price: 120m,
                    quantity: 10,
                    discountPercentage: 25m);

        CartIntegrationTestHelper.TestCartContext cart =
            await CartIntegrationTestHelper
                .CreateCartAsync(
                    _factory,
                    marketplace.Customer.UserId);

        await CartIntegrationTestHelper
            .CreateCartItemAsync(
                _factory,
                cart.CartId,
                marketplace.Listing.ListingId,
                quantity: 2);

        SetToken(
            marketplace.Customer.AccessToken);

        var request =
            new UpdateCartItemQuantityRequest
            {
                Quantity = 4
            };

        HttpResponseMessage response =
            await _client.PutAsJsonAsync(
                $"/api/cart/items/" +
                $"{marketplace.Listing.ListingId}",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.OK);

        CartResponse? result =
            await response.Content
                .ReadFromJsonAsync<CartResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            marketplace.Customer.UserId,
            result.UserId);

        CartItemResponse item =
            Assert.Single(result.Items);

        Assert.Equal(
            marketplace.Listing.ListingId,
            item.ListingId);

        Assert.Equal(4, item.Quantity);
        Assert.Equal(10, item.AvailableQuantity);
        Assert.Equal(120m, item.UnitPrice);
        Assert.Equal(25m, item.DiscountPercentage);
        Assert.Equal(90m, item.EffectiveUnitPrice);

        Assert.Equal(480m, item.LineSubtotal);
        Assert.Equal(120m, item.LineDiscount);
        Assert.Equal(360m, item.LineTotal);

        Assert.Equal(4, result.TotalItems);
        Assert.Equal(480m, result.Subtotal);
        Assert.Equal(120m, result.TotalDiscount);
        Assert.Equal(360m, result.FinalTotal);

        CartItem? storedItem =
            await CartIntegrationTestHelper
                .GetCartItemAsync(
                    _factory,
                    cart.CartId,
                    marketplace.Listing.ListingId);

        Assert.NotNull(storedItem);
        Assert.Equal(4, storedItem.Quantity);
    }

    [Fact]
    public async Task UpdateCartItem_WhenItemDoesNotExist_ReturnsNotFound()
    {
        CartIntegrationTestHelper.TestCartMarketplaceContext marketplace =
            await CartIntegrationTestHelper
                .CreateMarketplaceContextAsync(
                    _factory);

        await CartIntegrationTestHelper
            .CreateCartAsync(
                _factory,
                marketplace.Customer.UserId);

        SetToken(
            marketplace.Customer.AccessToken);

        var request =
            new UpdateCartItemQuantityRequest
            {
                Quantity = 2
            };

        HttpResponseMessage response =
            await _client.PutAsJsonAsync(
                "/api/cart/items/999999",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.NotFound);

        Assert.Equal(
            0,
            await CartIntegrationTestHelper
                .GetCartItemCountForUserAsync(
                    _factory,
                    marketplace.Customer.UserId));
    }

    [Fact]
    public async Task UpdateCartItem_WhenQuantityExceedsStock_ReturnsBadRequestAndKeepsQuantity()
    {
        CartIntegrationTestHelper.TestCartMarketplaceContext marketplace =
            await CartIntegrationTestHelper
                .CreateMarketplaceContextAsync(
                    _factory,
                    quantity: 5);

        CartIntegrationTestHelper.TestCartContext cart =
            await CartIntegrationTestHelper
                .CreateCartAsync(
                    _factory,
                    marketplace.Customer.UserId);

        await CartIntegrationTestHelper
            .CreateCartItemAsync(
                _factory,
                cart.CartId,
                marketplace.Listing.ListingId,
                quantity: 2);

        SetToken(
            marketplace.Customer.AccessToken);

        var request =
            new UpdateCartItemQuantityRequest
            {
                Quantity = 6
            };

        HttpResponseMessage response =
            await _client.PutAsJsonAsync(
                $"/api/cart/items/" +
                $"{marketplace.Listing.ListingId}",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.BadRequest);

        CartItem? storedItem =
            await CartIntegrationTestHelper
                .GetCartItemAsync(
                    _factory,
                    cart.CartId,
                    marketplace.Listing.ListingId);

        Assert.NotNull(storedItem);
        Assert.Equal(2, storedItem.Quantity);
    }

    [Fact]
    public async Task UpdateCartItem_WhenListingIsUnavailable_ReturnsBadRequestAndKeepsQuantity()
    {
        CartIntegrationTestHelper.TestCartMarketplaceContext marketplace =
            await CartIntegrationTestHelper
                .CreateMarketplaceContextAsync(
                    _factory,
                    quantity: 5,
                    status:
                        ElectronicLibrary.DAL.Enums
                            .ListingStatus.Suspended);

        CartIntegrationTestHelper.TestCartContext cart =
            await CartIntegrationTestHelper
                .CreateCartAsync(
                    _factory,
                    marketplace.Customer.UserId);

        await CartIntegrationTestHelper
            .CreateCartItemAsync(
                _factory,
                cart.CartId,
                marketplace.Listing.ListingId,
                quantity: 1);

        SetToken(
            marketplace.Customer.AccessToken);

        var request =
            new UpdateCartItemQuantityRequest
            {
                Quantity = 2
            };

        HttpResponseMessage response =
            await _client.PutAsJsonAsync(
                $"/api/cart/items/" +
                $"{marketplace.Listing.ListingId}",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.BadRequest);

        CartItem? storedItem =
            await CartIntegrationTestHelper
                .GetCartItemAsync(
                    _factory,
                    cart.CartId,
                    marketplace.Listing.ListingId);

        Assert.NotNull(storedItem);
        Assert.Equal(1, storedItem.Quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task UpdateCartItem_WithInvalidQuantity_ReturnsBadRequestAndKeepsQuantity(
        int quantity)
    {
        CartIntegrationTestHelper.TestCartMarketplaceContext marketplace =
            await CartIntegrationTestHelper
                .CreateMarketplaceContextAsync(
                    _factory,
                    quantity: 5);

        CartIntegrationTestHelper.TestCartContext cart =
            await CartIntegrationTestHelper
                .CreateCartAsync(
                    _factory,
                    marketplace.Customer.UserId);

        await CartIntegrationTestHelper
            .CreateCartItemAsync(
                _factory,
                cart.CartId,
                marketplace.Listing.ListingId,
                quantity: 1);

        SetToken(
            marketplace.Customer.AccessToken);

        var request =
            new UpdateCartItemQuantityRequest
            {
                Quantity = quantity
            };

        HttpResponseMessage response =
            await _client.PutAsJsonAsync(
                $"/api/cart/items/" +
                $"{marketplace.Listing.ListingId}",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.BadRequest);

        CartItem? storedItem =
            await CartIntegrationTestHelper
                .GetCartItemAsync(
                    _factory,
                    cart.CartId,
                    marketplace.Listing.ListingId);

        Assert.NotNull(storedItem);
        Assert.Equal(1, storedItem.Quantity);
    }

    [Fact]
    public async Task RemoveCartItem_WithoutToken_ReturnsUnauthorizedAndKeepsItem()
    {
        CartIntegrationTestHelper.TestCartMarketplaceContext marketplace =
            await CartIntegrationTestHelper
                .CreateMarketplaceContextAsync(
                    _factory,
                    quantity: 10);

        CartIntegrationTestHelper.TestCartContext cart =
            await CartIntegrationTestHelper
                .CreateCartAsync(
                    _factory,
                    marketplace.Customer.UserId);

        await CartIntegrationTestHelper
            .CreateCartItemAsync(
                _factory,
                cart.CartId,
                marketplace.Listing.ListingId,
                quantity: 2);

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        HttpResponseMessage response =
            await _client.DeleteAsync(
                $"/api/cart/items/" +
                $"{marketplace.Listing.ListingId}");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.Unauthorized);

        CartItem? storedItem =
            await CartIntegrationTestHelper
                .GetCartItemAsync(
                    _factory,
                    cart.CartId,
                    marketplace.Listing.ListingId);

        Assert.NotNull(storedItem);
        Assert.Equal(2, storedItem.Quantity);
    }

    [Fact]
    public async Task RemoveCartItem_WhenItemExists_RemovesRequestedItem()
    {
        CartIntegrationTestHelper.TestCartMarketplaceContext marketplace =
            await CartIntegrationTestHelper
                .CreateMarketplaceContextAsync(
                    _factory,
                    quantity: 10);

        CartIntegrationTestHelper.TestCartContext cart =
            await CartIntegrationTestHelper
                .CreateCartAsync(
                    _factory,
                    marketplace.Customer.UserId);

        await CartIntegrationTestHelper
            .CreateCartItemAsync(
                _factory,
                cart.CartId,
                marketplace.Listing.ListingId,
                quantity: 2);

        SetToken(
            marketplace.Customer.AccessToken);

        HttpResponseMessage response =
            await _client.DeleteAsync(
                $"/api/cart/items/" +
                $"{marketplace.Listing.ListingId}");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.NoContent);

        CartItem? storedItem =
            await CartIntegrationTestHelper
                .GetCartItemAsync(
                    _factory,
                    cart.CartId,
                    marketplace.Listing.ListingId);

        Assert.Null(storedItem);

        Cart? storedCart =
            await CartIntegrationTestHelper
                .GetCartByUserIdAsync(
                    _factory,
                    marketplace.Customer.UserId);

        Assert.NotNull(storedCart);
        Assert.Equal(cart.CartId, storedCart.CartId);
    }

    [Fact]
    public async Task RemoveCartItem_WhenItemDoesNotExist_ReturnsNotFound()
    {
        CartIntegrationTestHelper.TestCartMarketplaceContext marketplace =
            await CartIntegrationTestHelper
                .CreateMarketplaceContextAsync(
                    _factory);

        await CartIntegrationTestHelper
            .CreateCartAsync(
                _factory,
                marketplace.Customer.UserId);

        SetToken(
            marketplace.Customer.AccessToken);

        HttpResponseMessage response =
            await _client.DeleteAsync(
                "/api/cart/items/999999");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveCartItem_DoesNotRemoveAnotherUsersItem()
    {
        CartIntegrationTestHelper.TestCartMarketplaceContext marketplace =
            await CartIntegrationTestHelper
                .CreateMarketplaceContextAsync(
                    _factory,
                    quantity: 10);

        ListingIntegrationTestHelper.TestUserContext anotherCustomer =
            await ListingIntegrationTestHelper
                .CreateCustomerAsync(_factory);

        CartIntegrationTestHelper.TestCartContext anotherCart =
            await CartIntegrationTestHelper
                .CreateCartAsync(
                    _factory,
                    anotherCustomer.UserId);

        await CartIntegrationTestHelper
            .CreateCartItemAsync(
                _factory,
                anotherCart.CartId,
                marketplace.Listing.ListingId,
                quantity: 3);

        SetToken(
            marketplace.Customer.AccessToken);

        HttpResponseMessage response =
            await _client.DeleteAsync(
                $"/api/cart/items/" +
                $"{marketplace.Listing.ListingId}");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.NotFound);

        CartItem? storedItem =
            await CartIntegrationTestHelper
                .GetCartItemAsync(
                    _factory,
                    anotherCart.CartId,
                    marketplace.Listing.ListingId);

        Assert.NotNull(storedItem);
        Assert.Equal(3, storedItem.Quantity);
    }

    [Fact]
    public async Task ClearCart_WithoutToken_ReturnsUnauthorizedAndKeepsItems()
    {
        CartIntegrationTestHelper.TestCartMarketplaceContext marketplace =
            await CartIntegrationTestHelper
                .CreateMarketplaceContextAsync(
                    _factory,
                    quantity: 10);

        CartIntegrationTestHelper.TestCartContext cart =
            await CartIntegrationTestHelper
                .CreateCartAsync(
                    _factory,
                    marketplace.Customer.UserId);

        await CartIntegrationTestHelper
            .CreateCartItemAsync(
                _factory,
                cart.CartId,
                marketplace.Listing.ListingId,
                quantity: 2);

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        HttpResponseMessage response =
            await _client.DeleteAsync(
                "/api/cart/items");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.Unauthorized);

        Assert.Equal(
            1,
            await CartIntegrationTestHelper
                .GetCartItemCountForUserAsync(
                    _factory,
                    marketplace.Customer.UserId));
    }

    [Fact]
    public async Task ClearCart_WhenCartContainsItems_RemovesAllItemsAndKeepsCart()
    {
        CartIntegrationTestHelper.TestCartMarketplaceContext firstMarketplace =
            await CartIntegrationTestHelper
                .CreateMarketplaceContextAsync(
                    _factory,
                    quantity: 10);

        ListingIntegrationTestHelper.TestBookContext secondBook =
            await ListingIntegrationTestHelper
                .CreateBookAsync(_factory);

        ListingIntegrationTestHelper.TestListingContext secondListing =
            await ListingIntegrationTestHelper
                .CreateListingAsync(
                    _factory,
                    firstMarketplace.Seller.UserId,
                    secondBook.BookId,
                    quantity: 10,
                    status:
                        ElectronicLibrary.DAL.Enums
                            .ListingStatus.Active);

        CartIntegrationTestHelper.TestCartContext cart =
            await CartIntegrationTestHelper
                .CreateCartAsync(
                    _factory,
                    firstMarketplace.Customer.UserId);

        await CartIntegrationTestHelper
            .CreateCartItemAsync(
                _factory,
                cart.CartId,
                firstMarketplace.Listing.ListingId,
                quantity: 2);

        await CartIntegrationTestHelper
            .CreateCartItemAsync(
                _factory,
                cart.CartId,
                secondListing.ListingId,
                quantity: 3);

        SetToken(
            firstMarketplace.Customer.AccessToken);

        HttpResponseMessage response =
            await _client.DeleteAsync(
                "/api/cart/items");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.NoContent);

        Assert.Equal(
            0,
            await CartIntegrationTestHelper
                .GetCartItemCountForUserAsync(
                    _factory,
                    firstMarketplace.Customer.UserId));

        Cart? storedCart =
            await CartIntegrationTestHelper
                .GetCartByUserIdAsync(
                    _factory,
                    firstMarketplace.Customer.UserId);

        Assert.NotNull(storedCart);
        Assert.Equal(cart.CartId, storedCart.CartId);
    }

    [Fact]
    public async Task ClearCart_WhenCartDoesNotExist_ReturnsNoContentWithoutCreatingCart()
    {
        ListingIntegrationTestHelper.TestUserContext customer =
            await ListingIntegrationTestHelper
                .CreateCustomerAsync(_factory);

        SetToken(customer.AccessToken);

        HttpResponseMessage response =
            await _client.DeleteAsync(
                "/api/cart/items");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.NoContent);

        Assert.Equal(
            0,
            await CartIntegrationTestHelper
                .GetCartCountForUserAsync(
                    _factory,
                    customer.UserId));
    }

    [Fact]
    public async Task ClearCart_RemovesOnlyCurrentUsersItems()
    {
        CartIntegrationTestHelper.TestCartMarketplaceContext marketplace =
            await CartIntegrationTestHelper
                .CreateMarketplaceContextAsync(
                    _factory,
                    quantity: 10);

        ListingIntegrationTestHelper.TestUserContext anotherCustomer =
            await ListingIntegrationTestHelper
                .CreateCustomerAsync(_factory);

        CartIntegrationTestHelper.TestCartContext currentCart =
            await CartIntegrationTestHelper
                .CreateCartAsync(
                    _factory,
                    marketplace.Customer.UserId);

        CartIntegrationTestHelper.TestCartContext anotherCart =
            await CartIntegrationTestHelper
                .CreateCartAsync(
                    _factory,
                    anotherCustomer.UserId);

        await CartIntegrationTestHelper
            .CreateCartItemAsync(
                _factory,
                currentCart.CartId,
                marketplace.Listing.ListingId,
                quantity: 2);

        await CartIntegrationTestHelper
            .CreateCartItemAsync(
                _factory,
                anotherCart.CartId,
                marketplace.Listing.ListingId,
                quantity: 4);

        SetToken(
            marketplace.Customer.AccessToken);

        HttpResponseMessage response =
            await _client.DeleteAsync(
                "/api/cart/items");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.NoContent);

        Assert.Equal(
            0,
            await CartIntegrationTestHelper
                .GetCartItemCountForUserAsync(
                    _factory,
                    marketplace.Customer.UserId));

        CartItem? anotherItem =
            await CartIntegrationTestHelper
                .GetCartItemAsync(
                    _factory,
                    anotherCart.CartId,
                    marketplace.Listing.ListingId);

        Assert.NotNull(anotherItem);
        Assert.Equal(4, anotherItem.Quantity);
    }

    [Fact]
    public async Task GetCart_WhenListingIsSoftDeleted_KeepsItemVisibleAndMarksItUnavailable()
    {
        CartIntegrationTestHelper.TestCartMarketplaceContext marketplace =
            await CartIntegrationTestHelper
                .CreateMarketplaceContextAsync(
                    _factory,
                    price: 100m,
                    quantity: 5,
                    listingIsDeleted: true);

        CartIntegrationTestHelper.TestCartContext cart =
            await CartIntegrationTestHelper
                .CreateCartAsync(
                    _factory,
                    marketplace.Customer.UserId);

        await CartIntegrationTestHelper
            .CreateCartItemAsync(
                _factory,
                cart.CartId,
                marketplace.Listing.ListingId,
                quantity: 2);

        SetToken(
            marketplace.Customer.AccessToken);

        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/cart");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.OK);

        CartResponse? result =
            await response.Content
                .ReadFromJsonAsync<CartResponse>();

        Assert.NotNull(result);

        CartItemResponse item =
            Assert.Single(result.Items);

        Assert.Equal(
            marketplace.Listing.ListingId,
            item.ListingId);

        Assert.Equal(
            marketplace.Book.BookId,
            item.BookId);

        Assert.Equal(2, item.Quantity);
        Assert.False(item.IsAvailable);

        Assert.False(
            string.IsNullOrWhiteSpace(
                item.AvailabilityMessage));

        Assert.NotEqual(
            "ListingNotAvailable",
            item.AvailabilityMessage);

        Assert.Equal(
            1,
            await CartIntegrationTestHelper
                .GetCartItemCountForUserAsync(
                    _factory,
                    marketplace.Customer.UserId));
    }

    [Fact]
    public async Task GetCart_WhenBookIsSoftDeleted_KeepsItemVisibleAndMarksItUnavailable()
    {
        CartIntegrationTestHelper.TestCartMarketplaceContext marketplace =
            await CartIntegrationTestHelper
                .CreateMarketplaceContextAsync(
                    _factory,
                    price: 75m,
                    quantity: 5,
                    bookIsDeleted: true);

        CartIntegrationTestHelper.TestCartContext cart =
            await CartIntegrationTestHelper
                .CreateCartAsync(
                    _factory,
                    marketplace.Customer.UserId);

        await CartIntegrationTestHelper
            .CreateCartItemAsync(
                _factory,
                cart.CartId,
                marketplace.Listing.ListingId,
                quantity: 1);

        SetToken(
            marketplace.Customer.AccessToken);

        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/cart");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.OK);

        CartResponse? result =
            await response.Content
                .ReadFromJsonAsync<CartResponse>();

        Assert.NotNull(result);

        CartItemResponse item =
            Assert.Single(result.Items);

        Assert.Equal(
            marketplace.Book.BookId,
            item.BookId);

        Assert.Equal(
            marketplace.Book.Title,
            item.BookTitle);

        Assert.False(item.IsAvailable);

        Assert.False(
            string.IsNullOrWhiteSpace(
                item.AvailabilityMessage));

        Assert.NotEqual(
            "ListingBookNotAvailable",
            item.AvailabilityMessage);

        Assert.Equal(
            1,
            await CartIntegrationTestHelper
                .GetCartItemCountForUserAsync(
                    _factory,
                    marketplace.Customer.UserId));
    }

    [Fact]
    public async Task GetCart_WhenCartQuantityExceedsCurrentStock_MarksItemUnavailable()
    {
        CartIntegrationTestHelper.TestCartMarketplaceContext marketplace =
            await CartIntegrationTestHelper
                .CreateMarketplaceContextAsync(
                    _factory,
                    price: 50m,
                    quantity: 2);

        CartIntegrationTestHelper.TestCartContext cart =
            await CartIntegrationTestHelper
                .CreateCartAsync(
                    _factory,
                    marketplace.Customer.UserId);

        await CartIntegrationTestHelper
            .CreateCartItemAsync(
                _factory,
                cart.CartId,
                marketplace.Listing.ListingId,
                quantity: 4);

        SetToken(
            marketplace.Customer.AccessToken);

        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/cart");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.OK);

        CartResponse? result =
            await response.Content
                .ReadFromJsonAsync<CartResponse>();

        Assert.NotNull(result);

        CartItemResponse item =
            Assert.Single(result.Items);

        Assert.Equal(4, item.Quantity);
        Assert.Equal(2, item.AvailableQuantity);

        Assert.False(item.IsAvailable);

        Assert.False(
            string.IsNullOrWhiteSpace(
                item.AvailabilityMessage));

        Assert.NotEqual(
            "CartItemQuantityExceedsStock",
            item.AvailabilityMessage);

        CartItem? storedItem =
            await CartIntegrationTestHelper
                .GetCartItemAsync(
                    _factory,
                    cart.CartId,
                    marketplace.Listing.ListingId);

        Assert.NotNull(storedItem);
        Assert.Equal(4, storedItem.Quantity);
    }
}
