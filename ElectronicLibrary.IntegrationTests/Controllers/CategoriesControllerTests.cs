using ElectronicLibrary.DAL.Data;
using ElectronicLibrary.DAL.DTOs.Requests.Categories;
using ElectronicLibrary.DAL.DTOs.Responses.Categories;
using ElectronicLibrary.DAL.DTOs.Responses.Common;
using ElectronicLibrary.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace ElectronicLibrary.IntegrationTests.Controllers;

[Collection(IntegrationTestCollection.Name)]
public sealed class CategoriesControllerTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CategoriesControllerTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCategories_WithoutToken_ReturnsOk()
    {
        await ClearCategoriesAsync();

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/categories?pageNumber=1&pageSize=10");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        PagedResponse<CategoryResponse>? result =
            await response.Content.ReadFromJsonAsync<
                PagedResponse<CategoryResponse>>();

        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task GetCategoryById_WithMissingCategory_ReturnsNotFound()
    {
        await ClearCategoriesAsync();

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/categories/999999");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithoutToken_ReturnsUnauthorized()
    {
        await ClearCategoriesAsync();

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        var request = new CreateCategoryRequest
        {
            Name = "Unauthorized Category"
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/categories",
                request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        Assert.Equal(
            0,
            await GetCategoriesCountAsync());
    }

    [Fact]
    public async Task CreateCategory_AsCustomer_ReturnsForbidden()
    {
        await ClearCategoriesAsync();

        string customerToken =
            await IntegrationTestAuthenticationHelper
                .CreateAccessTokenAsync(
                    _factory,
                    "Customer");

        IntegrationTestAuthenticationHelper
            .SetBearerToken(
                _client,
                customerToken);

        var request = new CreateCategoryRequest
        {
            Name = "Forbidden Category"
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/categories",
                request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        Assert.Equal(
            0,
            await GetCategoriesCountAsync());
    }

    [Fact]
    public async Task CreateCategory_AsAdmin_ReturnsCreated()
    {
        await ClearCategoriesAsync();

        await AuthenticateAsAdminAsync();

        var request = new CreateCategoryRequest
        {
            Name = "  Science Fiction  ",
            Description = "  Fiction based on science.  "
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/categories",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        CategoryResponse? result =
            await response.Content
                .ReadFromJsonAsync<CategoryResponse>();

        Assert.NotNull(result);
        Assert.True(result.CategoryId > 0);
        Assert.Equal(
            "Science Fiction",
            result.Name);
        Assert.Equal(
            "Fiction based on science.",
            result.Description);
        Assert.Equal(0, result.BooksCount);

        Assert.NotNull(response.Headers.Location);

        Assert.EndsWith(
            $"/api/categories/{result.CategoryId}",
            response.Headers.Location.ToString());

        using IServiceScope scope =
            _factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var savedCategory =
            await dbContext.Categories
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            "Science Fiction",
            savedCategory.Name);

        Assert.False(
            string.IsNullOrWhiteSpace(
                savedCategory.CreatedById));

        Assert.False(savedCategory.IsDeleted);
    }

    [Fact]
    public async Task CreateCategory_WithEmptyName_ReturnsBadRequest()
    {
        await ClearCategoriesAsync();

        await AuthenticateAsAdminAsync();

        var request = new CreateCategoryRequest
        {
            Name = string.Empty,
            Description = "Invalid category"
        };

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/categories",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.Equal(
            0,
            await GetCategoriesCountAsync());
    }

    [Fact]
    public async Task CreateCategory_WithDuplicateName_ReturnsConflict()
    {
        await ClearCategoriesAsync();

        await AuthenticateAsAdminAsync();

        HttpResponseMessage firstResponse =
            await _client.PostAsJsonAsync(
                "/api/categories",
                new CreateCategoryRequest
                {
                    Name = "Science Fiction"
                });

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        HttpResponseMessage duplicateResponse =
            await _client.PostAsJsonAsync(
                "/api/categories",
                new CreateCategoryRequest
                {
                    Name = "science fiction"
                });

        Assert.Equal(
            HttpStatusCode.Conflict,
            duplicateResponse.StatusCode);

        Assert.Equal(
            1,
            await GetCategoriesCountAsync());
    }

    [Fact]
    public async Task UpdateCategory_WithMissingCategory_ReturnsNotFound()
    {
        await ClearCategoriesAsync();

        await AuthenticateAsAdminAsync();

        var request = new UpdateCategoryRequest
        {
            Name = "Missing Category"
        };

        HttpResponseMessage response =
            await _client.PutAsJsonAsync(
                "/api/categories/999999",
                request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task GetCategories_WithSearchAndPagination_ReturnsMatchingPage()
    {
        await ClearCategoriesAsync();

        await AuthenticateAsAdminAsync();

        string[] names =
        [
            "Science Fiction",
            "Historical Fiction",
            "Biography",
            "Fantasy"
        ];

        foreach (string name in names)
        {
            HttpResponseMessage createResponse =
                await _client.PostAsJsonAsync(
                    "/api/categories",
                    new CreateCategoryRequest
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
                "/api/categories" +
                "?search=Fiction" +
                "&pageNumber=1" +
                "&pageSize=1");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        PagedResponse<CategoryResponse>? result =
            await response.Content.ReadFromJsonAsync<
                PagedResponse<CategoryResponse>>();

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(1, result.PageSize);
        Assert.True(result.HasNextPage);

        Assert.Equal(
            "Historical Fiction",
            result.Items.Single().Name);
    }

    [Theory]
    [InlineData(
        "/api/categories?pageNumber=0&pageSize=10")]
    [InlineData(
        "/api/categories?pageNumber=1&pageSize=100")]
    public async Task GetCategories_WithInvalidPagination_ReturnsBadRequest(
        string requestUrl)
    {
        await ClearCategoriesAsync();

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        HttpResponseMessage response =
            await _client.GetAsync(requestUrl);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_AsAdmin_SoftDeletesCategory()
    {
        await ClearCategoriesAsync();

        await AuthenticateAsAdminAsync();

        HttpResponseMessage createResponse =
            await _client.PostAsJsonAsync(
                "/api/categories",
                new CreateCategoryRequest
                {
                    Name = "Category To Delete",
                    Description = "Temporary category"
                });

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        CategoryResponse? createdCategory =
            await createResponse.Content
                .ReadFromJsonAsync<CategoryResponse>();

        Assert.NotNull(createdCategory);

        HttpResponseMessage deleteResponse =
            await _client.DeleteAsync(
                $"/api/categories/" +
                $"{createdCategory.CategoryId}");

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
                $"/api/categories/" +
                $"{createdCategory.CategoryId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            getByIdResponse.StatusCode);

        HttpResponseMessage listResponse =
            await _client.GetAsync(
                "/api/categories?pageNumber=1&pageSize=10");

        PagedResponse<CategoryResponse>? listResult =
            await listResponse.Content.ReadFromJsonAsync<
                PagedResponse<CategoryResponse>>();

        Assert.NotNull(listResult);

        Assert.DoesNotContain(
            listResult.Items,
            category =>
                category.CategoryId ==
                createdCategory.CategoryId);

        using IServiceScope scope =
            _factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var deletedCategory =
            await dbContext.Categories
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(
                    category =>
                        category.CategoryId ==
                        createdCategory.CategoryId);

        Assert.True(deletedCategory.IsDeleted);
        Assert.NotNull(deletedCategory.DeletedAt);

        Assert.False(
            string.IsNullOrWhiteSpace(
                deletedCategory.DeletedById));
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

    private async Task<int> GetCategoriesCountAsync()
    {
        using IServiceScope scope =
            _factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        return await dbContext.Categories
            .IgnoreQueryFilters()
            .CountAsync();
    }

    private async Task ClearCategoriesAsync()
    {
        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        using IServiceScope scope =
            _factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        await dbContext.BookCategories
            .ExecuteDeleteAsync();

        await dbContext.Categories
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync();
    }
}