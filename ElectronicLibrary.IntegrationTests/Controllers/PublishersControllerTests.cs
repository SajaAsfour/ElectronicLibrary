using ElectronicLibrary.DAL.Data;
using ElectronicLibrary.DAL.DTOs.Requests.Publishers;
using ElectronicLibrary.DAL.DTOs.Responses.Common;
using ElectronicLibrary.DAL.DTOs.Responses.Publishers;
using ElectronicLibrary.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace ElectronicLibrary.IntegrationTests.Controllers;

[Collection(IntegrationTestCollection.Name)]
public sealed class PublishersControllerTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PublishersControllerTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPublishers_WithoutToken_ReturnsOk()
    {
        await ClearPublishersAsync();

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/publishers?pageNumber=1&pageSize=10");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        PagedResponse<PublisherResponse>? result =
            await response.Content.ReadFromJsonAsync<
                PagedResponse<PublisherResponse>>();

        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetPublisherById_WithMissingPublisher_ReturnsNotFound()
    {
        await ClearPublishersAsync();

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/publishers/999999");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task CreatePublisher_WithoutToken_ReturnsUnauthorized()
    {
        await ClearPublishersAsync();

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        var request = new CreatePublisherRequest
        {
            Name = "Unauthorized Publisher",
            Website = "https://example.com"
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/publishers",
                request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        Assert.Equal(
            0,
            await GetPublishersCountAsync());
    }

    [Fact]
    public async Task CreatePublisher_AsCustomer_ReturnsForbidden()
    {
        await ClearPublishersAsync();

        string customerToken =
            await IntegrationTestAuthenticationHelper
                .CreateAccessTokenAsync(
                    _factory,
                    "Customer");

        IntegrationTestAuthenticationHelper
            .SetBearerToken(
                _client,
                customerToken);

        var request = new CreatePublisherRequest
        {
            Name = "Forbidden Publisher",
            Website = "https://example.com"
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/publishers",
                request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        Assert.Equal(
            0,
            await GetPublishersCountAsync());
    }

    [Fact]
    public async Task CreatePublisher_AsAdmin_ReturnsCreated()
    {
        await ClearPublishersAsync();

        await AuthenticateAsAdminAsync();

        var request = new CreatePublisherRequest
        {
            Name = "Penguin Books",
            Website = "https://www.penguin.com"
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/publishers",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        PublisherResponse? result =
            await response.Content
                .ReadFromJsonAsync<PublisherResponse>();

        Assert.NotNull(result);
        Assert.True(result.PublisherId > 0);
        Assert.Equal(
            "Penguin Books",
            result.Name);
        Assert.Equal(
            "https://www.penguin.com",
            result.Website);
        Assert.Equal(0, result.BooksCount);

        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith(
            $"/api/publishers/{result.PublisherId}",
            response.Headers.Location.ToString());

        using IServiceScope scope =
            _factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var savedPublisher =
            await dbContext.Publishers
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            "Penguin Books",
            savedPublisher.Name);

        Assert.False(
            string.IsNullOrWhiteSpace(
                savedPublisher.CreatedById));

        Assert.False(savedPublisher.IsDeleted);
    }

    [Fact]
    public async Task CreatePublisher_WithEmptyName_ReturnsBadRequest()
    {
        await ClearPublishersAsync();

        await AuthenticateAsAdminAsync();

        var request = new CreatePublisherRequest
        {
            Name = string.Empty,
            Website = "https://example.com"
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/publishers",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            0,
            await GetPublishersCountAsync());
    }

    [Fact]
    public async Task CreatePublisher_WithInvalidWebsite_ReturnsBadRequest()
    {
        await ClearPublishersAsync();

        await AuthenticateAsAdminAsync();

        var request = new CreatePublisherRequest
        {
            Name = "Invalid Website Publisher",
            Website = "not-a-valid-url"
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/publishers",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            0,
            await GetPublishersCountAsync());
    }

    [Fact]
    public async Task CreatePublisher_WithDuplicateName_ReturnsConflict()
    {
        await ClearPublishersAsync();

        await AuthenticateAsAdminAsync();

        HttpResponseMessage firstResponse =
            await _client.PostAsJsonAsync(
                "/api/publishers",
                new CreatePublisherRequest
                {
                    Name = "Penguin Books",
                    Website = "https://www.penguin.com"
                });

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        HttpResponseMessage duplicateResponse =
            await _client.PostAsJsonAsync(
                "/api/publishers",
                new CreatePublisherRequest
                {
                    Name = "penguin books",
                    Website = "https://duplicate.example.com"
                });

        Assert.Equal(
            HttpStatusCode.Conflict,
            duplicateResponse.StatusCode);

        Assert.Equal(
            1,
            await GetPublishersCountAsync());
    }

    [Fact]
    public async Task UpdatePublisher_WithMissingPublisher_ReturnsNotFound()
    {
        await ClearPublishersAsync();

        await AuthenticateAsAdminAsync();

        var request = new UpdatePublisherRequest
        {
            Name = "Missing Publisher",
            Website = "https://example.com"
        };

        HttpResponseMessage response =
            await _client.PutAsJsonAsync(
                "/api/publishers/999999",
                request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task GetPublishers_WithSearchAndPagination_ReturnsMatchingPage()
    {
        await ClearPublishersAsync();

        await AuthenticateAsAdminAsync();

        string[] names =
        [
            "Penguin Books",
            "Penguin Random House",
            "Oxford University Press",
            "HarperCollins"
        ];

        foreach (string name in names)
        {
            HttpResponseMessage createResponse =
                await _client.PostAsJsonAsync(
                    "/api/publishers",
                    new CreatePublisherRequest
                    {
                        Name = name
                    });

            Assert.Equal(
                HttpStatusCode.Created,
                createResponse.StatusCode);
        }

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/publishers" +
                "?search=Penguin" +
                "&pageNumber=1" +
                "&pageSize=1");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        PagedResponse<PublisherResponse>? result =
            await response.Content.ReadFromJsonAsync<
                PagedResponse<PublisherResponse>>();

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(1, result.PageSize);
        Assert.True(result.HasNextPage);

        Assert.Equal(
            "Penguin Books",
            result.Items.Single().Name);
    }

    [Fact]
    public async Task DeletePublisher_AsAdmin_SoftDeletesPublisher()
    {
        await ClearPublishersAsync();

        await AuthenticateAsAdminAsync();

        HttpResponseMessage createResponse =
            await _client.PostAsJsonAsync(
                "/api/publishers",
                new CreatePublisherRequest
                {
                    Name = "Publisher To Delete",
                    Website = "https://delete.example.com"
                });

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        PublisherResponse? createdPublisher =
            await createResponse.Content
                .ReadFromJsonAsync<PublisherResponse>();

        Assert.NotNull(createdPublisher);

        HttpResponseMessage deleteResponse =
            await _client.DeleteAsync(
                $"/api/publishers/" +
                $"{createdPublisher.PublisherId}");

        Assert.Equal(
            HttpStatusCode.OK,
            deleteResponse.StatusCode);

        MessageResponse? deleteResult =
            await deleteResponse.Content
                .ReadFromJsonAsync<MessageResponse>();

        Assert.NotNull(deleteResult);
        Assert.False(
            string.IsNullOrWhiteSpace(
                deleteResult.Message));

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        HttpResponseMessage getByIdResponse =
            await _client.GetAsync(
                $"/api/publishers/" +
                $"{createdPublisher.PublisherId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            getByIdResponse.StatusCode);

        HttpResponseMessage listResponse =
            await _client.GetAsync(
                "/api/publishers?pageNumber=1&pageSize=10");

        PagedResponse<PublisherResponse>? listResult =
            await listResponse.Content.ReadFromJsonAsync<
                PagedResponse<PublisherResponse>>();

        Assert.NotNull(listResult);

        Assert.DoesNotContain(
            listResult.Items,
            publisher =>
                publisher.PublisherId ==
                createdPublisher.PublisherId);

        using IServiceScope scope =
            _factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var deletedPublisher =
            await dbContext.Publishers
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(
                    publisher =>
                        publisher.PublisherId ==
                        createdPublisher.PublisherId);

        Assert.True(deletedPublisher.IsDeleted);
        Assert.NotNull(deletedPublisher.DeletedAt);

        Assert.False(
            string.IsNullOrWhiteSpace(
                deletedPublisher.DeletedById));
    }

    private async Task AuthenticateAsAdminAsync()
    {
        string adminToken =
            await IntegrationTestAuthenticationHelper
                .CreateAccessTokenAsync(
                    _factory,
                    "Admin");

        IntegrationTestAuthenticationHelper
            .SetBearerToken(
                _client,
                adminToken);
    }

    private async Task<int> GetPublishersCountAsync()
    {
        using IServiceScope scope =
            _factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        return await dbContext.Publishers
            .IgnoreQueryFilters()
            .CountAsync();
    }

    private async Task ClearPublishersAsync()
    {
        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        using IServiceScope scope =
            _factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        await dbContext.CartItems
            .ExecuteDeleteAsync();

        await dbContext.OrderItems
            .ExecuteDeleteAsync();

        await dbContext.Reviews
            .ExecuteDeleteAsync();

        await dbContext.Listings
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync();

        await dbContext.BookImages
            .ExecuteDeleteAsync();

        await dbContext.BookAuthors
            .ExecuteDeleteAsync();

        await dbContext.BookCategories
            .ExecuteDeleteAsync();

        await dbContext.Books
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync();

        await dbContext.Publishers
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync();
    }
}