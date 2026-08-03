using ElectronicLibrary.DAL.Data;
using ElectronicLibrary.DAL.DTOs.Requests.Authors;
using ElectronicLibrary.DAL.DTOs.Responses.Authors;
using ElectronicLibrary.DAL.DTOs.Responses.Common;
using ElectronicLibrary.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace ElectronicLibrary.IntegrationTests.Controllers;

[Collection(IntegrationTestCollection.Name)]
public sealed class AuthorsControllerTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthorsControllerTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAuthors_WithoutToken_ReturnsOk()
    {
        await ClearAuthorsAsync();

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/authors?pageNumber=1&pageSize=10");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        PagedResponse<AuthorResponse>? result =
            await response.Content
                .ReadFromJsonAsync<
                    PagedResponse<AuthorResponse>>();

        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetAuthorById_WithMissingAuthor_ReturnsNotFound()
    {
        await ClearAuthorsAsync();

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/authors/999999");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateAuthor_WithoutToken_ReturnsUnauthorized()
    {
        await ClearAuthorsAsync();

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        var request = new CreateAuthorRequest
        {
            Name = "Unauthorized Author"
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/authors",
                request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        Assert.Equal(
            0,
            await GetAuthorsCountAsync());
    }

    [Fact]
    public async Task CreateAuthor_AsCustomer_ReturnsForbidden()
    {
        await ClearAuthorsAsync();

        string customerToken =
            await IntegrationTestAuthenticationHelper
                .CreateAccessTokenAsync(
                    _factory,
                    "Customer");

        IntegrationTestAuthenticationHelper
            .SetBearerToken(
                _client,
                customerToken);

        var request = new CreateAuthorRequest
        {
            Name = "Forbidden Author"
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/authors",
                request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        Assert.Equal(
            0,
            await GetAuthorsCountAsync());
    }

    [Fact]
    public async Task CreateAuthor_AsAdmin_ReturnsCreated()
    {
        await ClearAuthorsAsync();

        await AuthenticateAsAdminAsync();

        var request = new CreateAuthorRequest
        {
            Name = "  Naguib Mahfouz  ",
            Biography = "  Egyptian novelist.  "
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/authors",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        AuthorResponse? result =
            await response.Content
                .ReadFromJsonAsync<AuthorResponse>();

        Assert.NotNull(result);
        Assert.True(result.AuthorId > 0);
        Assert.Equal(
            "Naguib Mahfouz",
            result.Name);
        Assert.Equal(
            "Egyptian novelist.",
            result.Biography);
        Assert.Equal(0, result.BooksCount);

        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith(
            $"/api/authors/{result.AuthorId}",
            response.Headers.Location.ToString());

        using IServiceScope scope =
            _factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var savedAuthor =
            await dbContext.Authors
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            "Naguib Mahfouz",
            savedAuthor.Name);
        Assert.False(
            string.IsNullOrWhiteSpace(
                savedAuthor.CreatedById));
        Assert.False(savedAuthor.IsDeleted);
    }

    [Fact]
    public async Task CreateAuthor_WithEmptyName_ReturnsBadRequest()
    {
        await ClearAuthorsAsync();

        await AuthenticateAsAdminAsync();

        var request = new CreateAuthorRequest
        {
            Name = string.Empty
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/authors",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            0,
            await GetAuthorsCountAsync());
    }

    [Fact]
    public async Task CreateAuthor_WithDuplicateName_ReturnsConflict()
    {
        await ClearAuthorsAsync();

        await AuthenticateAsAdminAsync();

        HttpResponseMessage firstResponse =
            await _client.PostAsJsonAsync(
                "/api/authors",
                new CreateAuthorRequest
                {
                    Name = "Naguib Mahfouz"
                });

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        HttpResponseMessage duplicateResponse =
            await _client.PostAsJsonAsync(
                "/api/authors",
                new CreateAuthorRequest
                {
                    Name = "naguib mahfouz"
                });

        Assert.Equal(
            HttpStatusCode.Conflict,
            duplicateResponse.StatusCode);

        Assert.Equal(
            1,
            await GetAuthorsCountAsync());
    }

    [Fact]
    public async Task UpdateAuthor_WithMissingAuthor_ReturnsNotFound()
    {
        await ClearAuthorsAsync();

        await AuthenticateAsAdminAsync();

        var request = new UpdateAuthorRequest
        {
            Name = "Missing Author"
        };

        HttpResponseMessage response =
            await _client.PutAsJsonAsync(
                "/api/authors/999999",
                request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAuthors_WithSearchAndPagination_ReturnsMatchingPage()
    {
        await ClearAuthorsAsync();

        await AuthenticateAsAdminAsync();

        string[] names =
        [
            "Mahmoud Darwish",
            "Mahmoud Shuqair",
            "Naguib Mahfouz",
            "Ghassan Kanafani"
        ];

        foreach (string name in names)
        {
            HttpResponseMessage createResponse =
                await _client.PostAsJsonAsync(
                    "/api/authors",
                    new CreateAuthorRequest
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
                "/api/authors" +
                "?search=Mahmoud" +
                "&pageNumber=1" +
                "&pageSize=1");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        PagedResponse<AuthorResponse>? result =
            await response.Content
                .ReadFromJsonAsync<
                    PagedResponse<AuthorResponse>>();

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(1, result.PageSize);
        Assert.True(result.HasNextPage);

        Assert.Equal(
            "Mahmoud Darwish",
            result.Items.Single().Name);
    }

    [Fact]
    public async Task DeleteAuthor_AsAdmin_SoftDeletesAuthor()
    {
        await ClearAuthorsAsync();

        await AuthenticateAsAdminAsync();

        HttpResponseMessage createResponse =
            await _client.PostAsJsonAsync(
                "/api/authors",
                new CreateAuthorRequest
                {
                    Name = "Author To Delete"
                });

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        AuthorResponse? createdAuthor =
            await createResponse.Content
                .ReadFromJsonAsync<AuthorResponse>();

        Assert.NotNull(createdAuthor);

        HttpResponseMessage deleteResponse =
            await _client.DeleteAsync(
                $"/api/authors/{createdAuthor.AuthorId}");

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
                $"/api/authors/{createdAuthor.AuthorId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            getByIdResponse.StatusCode);

        HttpResponseMessage listResponse =
            await _client.GetAsync(
                "/api/authors?pageNumber=1&pageSize=10");

        PagedResponse<AuthorResponse>? listResult =
            await listResponse.Content
                .ReadFromJsonAsync<
                    PagedResponse<AuthorResponse>>();

        Assert.NotNull(listResult);
        Assert.DoesNotContain(
            listResult.Items,
            author =>
                author.AuthorId ==
                createdAuthor.AuthorId);

        using IServiceScope scope =
            _factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var deletedAuthor =
            await dbContext.Authors
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(
                    author =>
                        author.AuthorId ==
                        createdAuthor.AuthorId);

        Assert.True(deletedAuthor.IsDeleted);
        Assert.NotNull(deletedAuthor.DeletedAt);
        Assert.False(
            string.IsNullOrWhiteSpace(
                deletedAuthor.DeletedById));
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

    private async Task<int> GetAuthorsCountAsync()
    {
        using IServiceScope scope =
            _factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        return await dbContext.Authors
            .IgnoreQueryFilters()
            .CountAsync();
    }

    private async Task ClearAuthorsAsync()
    {
        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        using IServiceScope scope =
            _factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        await dbContext.BookAuthors
            .ExecuteDeleteAsync();

        await dbContext.Authors
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync();
    }
}