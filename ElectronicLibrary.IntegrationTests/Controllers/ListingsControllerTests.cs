using ElectronicLibrary.DAL.DTOs.Requests.Listings;
using ElectronicLibrary.DAL.DTOs.Responses.Common;
using ElectronicLibrary.DAL.DTOs.Responses.Listings;
using ElectronicLibrary.DAL.Enums;
using ElectronicLibrary.DAL.Models.Marketplace;
using ElectronicLibrary.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace ElectronicLibrary.IntegrationTests.Controllers;

[Collection(IntegrationTestCollection.Name)]
public sealed class ListingsControllerTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ListingsControllerTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task
        CreateListing_WithoutToken_ReturnsUnauthorized()
    {
        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        ListingIntegrationTestHelper.TestBookContext book =
            await ListingIntegrationTestHelper
                .CreateBookAsync(_factory);

        CreateListingRequest request =
            CreatePhysicalListingRequest(
                book.BookId);

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/listings",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.Unauthorized);

        Assert.Equal(
            0,
            await ListingIntegrationTestHelper
                .GetListingCountIgnoringFiltersAsync(
                    _factory,
                    bookId: book.BookId));
    }

    [Fact]
    public async Task
        CreateListing_AsCustomer_ReturnsForbidden()
    {
        ListingIntegrationTestHelper.TestUserContext customer =
            await ListingIntegrationTestHelper
                .CreateCustomerAsync(_factory);

        SetToken(customer.AccessToken);

        ListingIntegrationTestHelper.TestBookContext book =
            await ListingIntegrationTestHelper
                .CreateBookAsync(_factory);

        CreateListingRequest request =
            CreatePhysicalListingRequest(
                book.BookId);

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/listings",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.Forbidden);

        Assert.Equal(
            0,
            await ListingIntegrationTestHelper
                .GetListingCountIgnoringFiltersAsync(
                    _factory,
                    bookId: book.BookId));
    }

    [Fact]
    public async Task
        CreateListing_AsSeller_ReturnsCreatedDraftListing()
    {
        ListingIntegrationTestHelper.TestUserContext seller =
            await ListingIntegrationTestHelper
                .CreateSellerAsync(
                    _factory,
                    "Integration Book Store");

        SetToken(seller.AccessToken);

        ListingIntegrationTestHelper.TestBookContext book =
            await ListingIntegrationTestHelper
                .CreateBookAsync(_factory);

        CreateListingRequest request =
            CreatePhysicalListingRequest(
                book.BookId,
                price: 100m,
                quantity: 5,
                discountPercentage: 10m,
                condition: BookCondition.LikeNew);

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/listings",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.Created);

        ListingResponse? result =
            await response.Content
                .ReadFromJsonAsync<ListingResponse>();

        Assert.NotNull(result);

        Assert.True(result.ListingId > 0);

        Assert.Equal(
            book.BookId,
            result.BookId);

        Assert.Equal(
            book.Title,
            result.BookTitle);

        Assert.Equal(
            seller.UserId,
            result.SellerId);

        Assert.Equal(
            "Integration Book Store",
            result.StoreName);

        Assert.Equal(
            100m,
            result.Price);

        Assert.Equal(
            10m,
            result.DiscountPercentage);

        Assert.Equal(
            90m,
            result.EffectivePrice);

        Assert.Equal(
            ListingStatus.Draft,
            result.Status);

        Assert.False(result.IsAvailable);

        Listing savedListing =
            await ListingIntegrationTestHelper
                .GetListingIgnoringFiltersAsync(
                    _factory,
                    result.ListingId);

        Assert.Equal(
            seller.UserId,
            savedListing.SellerId);

        Assert.Equal(
            seller.UserId,
            savedListing.CreatedById);

        Assert.Equal(
            ListingStatus.Draft,
            savedListing.Status);

        Assert.False(savedListing.IsDeleted);
    }

    [Fact]
    public async Task
        CreateListing_WithMissingBook_ReturnsNotFound()
    {
        ListingIntegrationTestHelper.TestUserContext seller =
            await ListingIntegrationTestHelper
                .CreateSellerAsync(_factory);

        SetToken(seller.AccessToken);

        CreateListingRequest request =
            CreatePhysicalListingRequest(
                bookId: 999999);

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/listings",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.NotFound);

        Assert.Equal(
            0,
            await ListingIntegrationTestHelper
                .GetListingCountIgnoringFiltersAsync(
                    _factory,
                    sellerId: seller.UserId));
    }

    [Fact]
    public async Task
        CreatePhysicalListing_WithoutCondition_ReturnsBadRequest()
    {
        ListingIntegrationTestHelper.TestUserContext seller =
            await ListingIntegrationTestHelper
                .CreateSellerAsync(_factory);

        SetToken(seller.AccessToken);

        ListingIntegrationTestHelper.TestBookContext book =
            await ListingIntegrationTestHelper
                .CreateBookAsync(_factory);

        var request = new CreateListingRequest
        {
            BookId = book.BookId,
            Price = 75m,
            Quantity = 3,
            Format = BookFormat.Physical,
            Condition = null,
            DiscountPercentage = 0m
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/listings",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.BadRequest);

        Assert.Equal(
            0,
            await ListingIntegrationTestHelper
                .GetListingCountIgnoringFiltersAsync(
                    _factory,
                    bookId: book.BookId));
    }

    [Fact]
    public async Task
        CreateDigitalListing_WithCondition_ReturnsBadRequest()
    {
        ListingIntegrationTestHelper.TestUserContext seller =
            await ListingIntegrationTestHelper
                .CreateSellerAsync(_factory);

        SetToken(seller.AccessToken);

        ListingIntegrationTestHelper.TestBookContext book =
            await ListingIntegrationTestHelper
                .CreateBookAsync(_factory);

        var request = new CreateListingRequest
        {
            BookId = book.BookId,
            Price = 40m,
            Quantity = 10,
            Format = BookFormat.Digital,
            Condition = BookCondition.New,
            DiscountPercentage = 0m
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/listings",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task
        GetListingById_WithActiveListing_ReturnsOkWithoutToken()
    {
        ListingIntegrationTestHelper.TestUserContext seller =
            await ListingIntegrationTestHelper
                .CreateSellerAsync(_factory);

        ListingIntegrationTestHelper.TestBookContext book =
            await ListingIntegrationTestHelper
                .CreateBookAsync(_factory);

        ListingIntegrationTestHelper.TestListingContext listing =
            await ListingIntegrationTestHelper
                .CreateListingAsync(
                    _factory,
                    seller.UserId,
                    book.BookId,
                    price: 120m,
                    quantity: 4,
                    discountPercentage: 25m,
                    status: ListingStatus.Active);

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        HttpResponseMessage response =
            await _client.GetAsync(
                $"/api/listings/{listing.ListingId}");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.OK);

        ListingResponse? result =
            await response.Content
                .ReadFromJsonAsync<ListingResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            listing.ListingId,
            result.ListingId);

        Assert.Equal(
            90m,
            result.EffectivePrice);

        Assert.Equal(
            ListingStatus.Active,
            result.Status);

        Assert.True(result.IsAvailable);
    }

    [Theory]
    [InlineData(ListingStatus.Draft, 5)]
    [InlineData(ListingStatus.OutOfStock, 0)]
    [InlineData(ListingStatus.Suspended, 5)]
    public async Task
        GetListingById_WithNonPublicListing_ReturnsNotFound(
            ListingStatus status,
            int quantity)
    {
        ListingIntegrationTestHelper.TestUserContext seller =
            await ListingIntegrationTestHelper
                .CreateSellerAsync(_factory);

        ListingIntegrationTestHelper.TestBookContext book =
            await ListingIntegrationTestHelper
                .CreateBookAsync(_factory);

        ListingIntegrationTestHelper.TestListingContext listing =
            await ListingIntegrationTestHelper
                .CreateListingAsync(
                    _factory,
                    seller.UserId,
                    book.BookId,
                    quantity: quantity,
                    status: status);

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        HttpResponseMessage response =
            await _client.GetAsync(
                $"/api/listings/{listing.ListingId}");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task
        UpdateListing_AsOwner_UpdatesValues()
    {
        ListingIntegrationTestHelper.TestUserContext seller =
            await ListingIntegrationTestHelper
                .CreateSellerAsync(_factory);

        ListingIntegrationTestHelper.TestBookContext book =
            await ListingIntegrationTestHelper
                .CreateBookAsync(_factory);

        ListingIntegrationTestHelper.TestListingContext listing =
            await ListingIntegrationTestHelper
                .CreateListingAsync(
                    _factory,
                    seller.UserId,
                    book.BookId);

        SetToken(seller.AccessToken);

        var request = new UpdateListingRequest
        {
            Price = 80m,
            Quantity = 7,
            Format = BookFormat.Physical,
            Condition = BookCondition.Good,
            DiscountPercentage = 25m
        };

        HttpResponseMessage response =
            await _client.PutAsJsonAsync(
                $"/api/listings/{listing.ListingId}",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.OK);

        ListingResponse? result =
            await response.Content
                .ReadFromJsonAsync<ListingResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            80m,
            result.Price);

        Assert.Equal(
            60m,
            result.EffectivePrice);

        Assert.Equal(
            7,
            result.Quantity);

        Assert.Equal(
            BookCondition.Good,
            result.Condition);

        Listing savedListing =
            await ListingIntegrationTestHelper
                .GetListingIgnoringFiltersAsync(
                    _factory,
                    listing.ListingId);

        Assert.NotNull(savedListing.UpdatedAt);

        Assert.Equal(
            seller.UserId,
            savedListing.UpdatedById);
    }

    [Fact]
    public async Task
        UpdateListing_AsAnotherSeller_ReturnsForbidden()
    {
        ListingIntegrationTestHelper.TestUserContext owner =
            await ListingIntegrationTestHelper
                .CreateSellerAsync(_factory);

        ListingIntegrationTestHelper.TestUserContext otherSeller =
            await ListingIntegrationTestHelper
                .CreateSellerAsync(_factory);

        ListingIntegrationTestHelper.TestBookContext book =
            await ListingIntegrationTestHelper
                .CreateBookAsync(_factory);

        ListingIntegrationTestHelper.TestListingContext listing =
            await ListingIntegrationTestHelper
                .CreateListingAsync(
                    _factory,
                    owner.UserId,
                    book.BookId);

        SetToken(otherSeller.AccessToken);

        var request = new UpdateListingRequest
        {
            Price = 70m,
            Quantity = 2,
            Format = BookFormat.Physical,
            Condition = BookCondition.Good,
            DiscountPercentage = 0m
        };

        HttpResponseMessage response =
            await _client.PutAsJsonAsync(
                $"/api/listings/{listing.ListingId}",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.Forbidden);

        Listing savedListing =
            await ListingIntegrationTestHelper
                .GetListingIgnoringFiltersAsync(
                    _factory,
                    listing.ListingId);

        Assert.Equal(
            100m,
            savedListing.Price);

        Assert.Equal(
            owner.UserId,
            savedListing.SellerId);
    }

    [Fact]
    public async Task
        UpdateActiveListing_WithZeroQuantity_ChangesToOutOfStock()
    {
        ListingIntegrationTestHelper.TestUserContext seller =
            await ListingIntegrationTestHelper
                .CreateSellerAsync(_factory);

        ListingIntegrationTestHelper.TestBookContext book =
            await ListingIntegrationTestHelper
                .CreateBookAsync(_factory);

        ListingIntegrationTestHelper.TestListingContext listing =
            await ListingIntegrationTestHelper
                .CreateListingAsync(
                    _factory,
                    seller.UserId,
                    book.BookId,
                    quantity: 5,
                    status: ListingStatus.Active);

        SetToken(seller.AccessToken);

        var request = new UpdateListingRequest
        {
            Price = 100m,
            Quantity = 0,
            Format = BookFormat.Physical,
            Condition = BookCondition.New,
            DiscountPercentage = 0m
        };

        HttpResponseMessage response =
            await _client.PutAsJsonAsync(
                $"/api/listings/{listing.ListingId}",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.OK);

        ListingResponse? result =
            await response.Content
                .ReadFromJsonAsync<ListingResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            ListingStatus.OutOfStock,
            result.Status);

        Assert.False(result.IsAvailable);
    }

    [Fact]
    public async Task
        UpdateListingStatus_FromDraftToActive_ReturnsOk()
    {
        ListingIntegrationTestHelper.TestUserContext seller =
            await ListingIntegrationTestHelper
                .CreateSellerAsync(_factory);

        ListingIntegrationTestHelper.TestBookContext book =
            await ListingIntegrationTestHelper
                .CreateBookAsync(_factory);

        ListingIntegrationTestHelper.TestListingContext listing =
            await ListingIntegrationTestHelper
                .CreateListingAsync(
                    _factory,
                    seller.UserId,
                    book.BookId,
                    quantity: 4,
                    status: ListingStatus.Draft);

        SetToken(seller.AccessToken);

        var request = new UpdateListingStatusRequest
        {
            Status = ListingStatus.Active
        };

        HttpResponseMessage response =
            await _client.PatchAsJsonAsync(
                $"/api/listings/{listing.ListingId}/status",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.OK);

        ListingResponse? result =
            await response.Content
                .ReadFromJsonAsync<ListingResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            ListingStatus.Active,
            result.Status);

        Assert.True(result.IsAvailable);
    }

    [Fact]
    public async Task
        UpdateListingStatus_ToActiveWithoutStock_ReturnsBadRequest()
    {
        ListingIntegrationTestHelper.TestUserContext seller =
            await ListingIntegrationTestHelper
                .CreateSellerAsync(_factory);

        ListingIntegrationTestHelper.TestBookContext book =
            await ListingIntegrationTestHelper
                .CreateBookAsync(_factory);

        ListingIntegrationTestHelper.TestListingContext listing =
            await ListingIntegrationTestHelper
                .CreateListingAsync(
                    _factory,
                    seller.UserId,
                    book.BookId,
                    quantity: 0,
                    status: ListingStatus.Draft);

        SetToken(seller.AccessToken);

        var request = new UpdateListingStatusRequest
        {
            Status = ListingStatus.Active
        };

        HttpResponseMessage response =
            await _client.PatchAsJsonAsync(
                $"/api/listings/{listing.ListingId}/status",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.BadRequest);

        Listing savedListing =
            await ListingIntegrationTestHelper
                .GetListingIgnoringFiltersAsync(
                    _factory,
                    listing.ListingId);

        Assert.Equal(
            ListingStatus.Draft,
            savedListing.Status);
    }

    [Fact]
    public async Task
        UpdateListingStatus_ToSuspended_ReturnsForbidden()
    {
        ListingIntegrationTestHelper.TestUserContext seller =
            await ListingIntegrationTestHelper
                .CreateSellerAsync(_factory);

        ListingIntegrationTestHelper.TestBookContext book =
            await ListingIntegrationTestHelper
                .CreateBookAsync(_factory);

        ListingIntegrationTestHelper.TestListingContext listing =
            await ListingIntegrationTestHelper
                .CreateListingAsync(
                    _factory,
                    seller.UserId,
                    book.BookId);

        SetToken(seller.AccessToken);

        var request = new UpdateListingStatusRequest
        {
            Status = ListingStatus.Suspended
        };

        HttpResponseMessage response =
            await _client.PatchAsJsonAsync(
                $"/api/listings/{listing.ListingId}/status",
                request);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task
        DeleteListing_AsOwner_SoftDeletesAndReturnsNoContent()
    {
        ListingIntegrationTestHelper.TestUserContext seller =
            await ListingIntegrationTestHelper
                .CreateSellerAsync(_factory);

        ListingIntegrationTestHelper.TestBookContext book =
            await ListingIntegrationTestHelper
                .CreateBookAsync(_factory);

        ListingIntegrationTestHelper.TestListingContext listing =
            await ListingIntegrationTestHelper
                .CreateListingAsync(
                    _factory,
                    seller.UserId,
                    book.BookId,
                    status: ListingStatus.Active);

        SetToken(seller.AccessToken);

        HttpResponseMessage response =
            await _client.DeleteAsync(
                $"/api/listings/{listing.ListingId}");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.NoContent);

        Listing deletedListing =
            await ListingIntegrationTestHelper
                .GetListingIgnoringFiltersAsync(
                    _factory,
                    listing.ListingId);

        Assert.True(deletedListing.IsDeleted);

        Assert.NotNull(deletedListing.DeletedAt);

        Assert.Equal(
            seller.UserId,
            deletedListing.DeletedById);

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        HttpResponseMessage publicResponse =
            await _client.GetAsync(
                $"/api/listings/{listing.ListingId}");

        await AssertStatusCodeAsync(
            publicResponse,
            HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task
        DeleteListing_AsAnotherSeller_ReturnsForbidden()
    {
        ListingIntegrationTestHelper.TestUserContext owner =
            await ListingIntegrationTestHelper
                .CreateSellerAsync(_factory);

        ListingIntegrationTestHelper.TestUserContext otherSeller =
            await ListingIntegrationTestHelper
                .CreateSellerAsync(_factory);

        ListingIntegrationTestHelper.TestBookContext book =
            await ListingIntegrationTestHelper
                .CreateBookAsync(_factory);

        ListingIntegrationTestHelper.TestListingContext listing =
            await ListingIntegrationTestHelper
                .CreateListingAsync(
                    _factory,
                    owner.UserId,
                    book.BookId);

        SetToken(otherSeller.AccessToken);

        HttpResponseMessage response =
            await _client.DeleteAsync(
                $"/api/listings/{listing.ListingId}");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.Forbidden);

        Listing savedListing =
            await ListingIntegrationTestHelper
                .GetListingIgnoringFiltersAsync(
                    _factory,
                    listing.ListingId);

        Assert.False(savedListing.IsDeleted);
    }

    [Fact]
    public async Task
        GetCurrentSellerListings_WithStatusAndPagination_ReturnsOwnedListings()
    {
        ListingIntegrationTestHelper.TestUserContext seller =
            await ListingIntegrationTestHelper
                .CreateSellerAsync(_factory);

        ListingIntegrationTestHelper.TestUserContext otherSeller =
            await ListingIntegrationTestHelper
                .CreateSellerAsync(_factory);

        ListingIntegrationTestHelper.TestBookContext book =
            await ListingIntegrationTestHelper
                .CreateBookAsync(_factory);

        for (int index = 0;
             index < 3;
             index++)
        {
            await ListingIntegrationTestHelper
                .CreateListingAsync(
                    _factory,
                    seller.UserId,
                    book.BookId,
                    price: 20m + index,
                    status: ListingStatus.Active);
        }

        await ListingIntegrationTestHelper
            .CreateListingAsync(
                _factory,
                seller.UserId,
                book.BookId,
                status: ListingStatus.Draft);

        await ListingIntegrationTestHelper
            .CreateListingAsync(
                _factory,
                otherSeller.UserId,
                book.BookId,
                status: ListingStatus.Active);

        SetToken(seller.AccessToken);

        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/listings/mine" +
                "?status=2&pageNumber=2&pageSize=2");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.OK);

        PagedResponse<ListingResponse>? result =
            await response.Content
                .ReadFromJsonAsync<
                    PagedResponse<ListingResponse>>();

        Assert.NotNull(result);

        Assert.Single(result.Items);

        Assert.Equal(
            3,
            result.TotalCount);

        Assert.Equal(
            2,
            result.TotalPages);

        Assert.Equal(
            2,
            result.PageNumber);

        Assert.True(result.HasPreviousPage);

        Assert.False(result.HasNextPage);

        Assert.All(
            result.Items,
            item =>
            {
                Assert.Equal(
                    seller.UserId,
                    item.SellerId);

                Assert.Equal(
                    ListingStatus.Active,
                    item.Status);
            });
    }

    [Fact]
    public async Task
        GetBookListings_ReturnsOnlyActiveInStockOrderedByEffectivePrice()
    {
        ListingIntegrationTestHelper.TestUserContext seller =
            await ListingIntegrationTestHelper
                .CreateSellerAsync(_factory);

        ListingIntegrationTestHelper.TestBookContext book =
            await ListingIntegrationTestHelper
                .CreateBookAsync(_factory);

        ListingIntegrationTestHelper.TestListingContext expensive =
            await ListingIntegrationTestHelper
                .CreateListingAsync(
                    _factory,
                    seller.UserId,
                    book.BookId,
                    price: 100m,
                    quantity: 5,
                    discountPercentage: 10m,
                    status: ListingStatus.Active);

        ListingIntegrationTestHelper.TestListingContext cheapest =
            await ListingIntegrationTestHelper
                .CreateListingAsync(
                    _factory,
                    seller.UserId,
                    book.BookId,
                    price: 80m,
                    quantity: 5,
                    discountPercentage: 25m,
                    status: ListingStatus.Active);

        await ListingIntegrationTestHelper
            .CreateListingAsync(
                _factory,
                seller.UserId,
                book.BookId,
                price: 20m,
                quantity: 5,
                status: ListingStatus.Draft);

        await ListingIntegrationTestHelper
            .CreateListingAsync(
                _factory,
                seller.UserId,
                book.BookId,
                price: 30m,
                quantity: 0,
                status: ListingStatus.OutOfStock);

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        HttpResponseMessage response =
            await _client.GetAsync(
                $"/api/books/{book.BookId}/listings" +
                "?pageNumber=1&pageSize=10");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.OK);

        PagedResponse<ListingResponse>? result =
            await response.Content
                .ReadFromJsonAsync<
                    PagedResponse<ListingResponse>>();

        Assert.NotNull(result);

        Assert.Equal(
            2,
            result.TotalCount);

        ListingResponse[] items =
            result.Items.ToArray();

        Assert.Equal(
            cheapest.ListingId,
            items[0].ListingId);

        Assert.Equal(
            60m,
            items[0].EffectivePrice);

        Assert.Equal(
            expensive.ListingId,
            items[1].ListingId);

        Assert.Equal(
            90m,
            items[1].EffectivePrice);

        Assert.All(
            items,
            item =>
            {
                Assert.Equal(
                    ListingStatus.Active,
                    item.Status);

                Assert.True(item.Quantity > 0);

                Assert.True(item.IsAvailable);
            });
    }

    [Fact]
    public async Task
        GetBookListings_WithInvalidPageSize_ReturnsBadRequest()
    {
        ListingIntegrationTestHelper.TestBookContext book =
            await ListingIntegrationTestHelper
                .CreateBookAsync(_factory);

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        HttpResponseMessage response =
            await _client.GetAsync(
                $"/api/books/{book.BookId}/listings" +
                "?pageNumber=1&pageSize=51");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.BadRequest);
    }

    private void SetToken(string accessToken)
    {
        IntegrationTestAuthenticationHelper
            .SetBearerToken(
                _client,
                accessToken);
    }

    private static CreateListingRequest
        CreatePhysicalListingRequest(
            int bookId,
            decimal price = 100m,
            int quantity = 5,
            decimal discountPercentage = 0m,
            BookCondition condition =
                BookCondition.New)
    {
        return new CreateListingRequest
        {
            BookId = bookId,
            Price = price,
            Quantity = quantity,
            Format = BookFormat.Physical,
            Condition = condition,
            DiscountPercentage =
                discountPercentage
        };
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
}