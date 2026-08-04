using ElectronicLibrary.DAL.Constants;
using ElectronicLibrary.DAL.Data;
using ElectronicLibrary.DAL.DTOs.Requests.Books;
using ElectronicLibrary.DAL.DTOs.Responses.Books;
using ElectronicLibrary.DAL.Models.Catalog;
using ElectronicLibrary.DAL.DTOs.Responses.Common;
using ElectronicLibrary.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace ElectronicLibrary.IntegrationTests.Controllers;

[Collection(IntegrationTestCollection.Name)]
public sealed class BooksControllerTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public BooksControllerTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetBooks_WithoutToken_ReturnsOk()
    {
        await ClearBookCatalogAsync();

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        HttpResponseMessage response =
            await _client.GetAsync("/api/books");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.OK);

        PagedResponse<BookResponse>? result =
        await response.Content
            .ReadFromJsonAsync<
                PagedResponse<BookResponse>>();

        Assert.NotNull(result);
        Assert.NotNull(result.Items);

    }

    [Fact]
    public async Task GetBookById_WithMissingBook_ReturnsNotFound()
    {
        await ClearBookCatalogAsync();

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/books/999999");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateBook_WithoutToken_ReturnsUnauthorized()
    {
        await ClearBookCatalogAsync();

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        CreateBookRequest request =
            CreateRequest(
                publisherId: 1,
                authorId: 1,
                categoryId: 1);

        using MultipartFormDataContent content =
            CreateBookMultipartContent(request);

        HttpResponseMessage response =
            await _client.PostAsync(
                "/api/books",
                content);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.Unauthorized);

        Assert.Equal(
            0,
            await GetBooksCountAsync());
    }

    [Fact]
    public async Task CreateBook_AsCustomer_ReturnsForbidden()
    {
        await ClearBookCatalogAsync();

        await AuthenticateAsync(
            ApplicationRoles.Customer);

        CreateBookRequest request =
            CreateRequest(
                publisherId: 1,
                authorId: 1,
                categoryId: 1);

        using MultipartFormDataContent content =
            CreateBookMultipartContent(request);

        HttpResponseMessage response =
            await _client.PostAsync(
                "/api/books",
                content);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.Forbidden);

        Assert.Equal(
            0,
            await GetBooksCountAsync());
    }

    [Fact]
    public async Task CreateBook_AsAdmin_ReturnsCreatedWithRelationshipsAndImage()
    {
        await ClearBookCatalogAsync();

        var catalog =
            await SeedCatalogAsync();

        await AuthenticateAsAdminAsync();

        CreateBookRequest request =
            CreateRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        using MultipartFormDataContent content =
            CreateBookMultipartContent(
                request,
                imageCount: 1,
                mainImageIndex: 0);

        HttpResponseMessage response =
            await _client.PostAsync(
                "/api/books",
                content);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.Created);

        BookDetailsResponse? result =
            await response.Content
                .ReadFromJsonAsync<BookDetailsResponse>();

        Assert.NotNull(result);
        Assert.True(result.BookId > 0);

        Assert.Equal(
            "The Cairo Trilogy",
            result.Title);

        Assert.Equal(
            "9780307947109",
            result.Isbn);

        Assert.Equal(
            "A literary trilogy set in Cairo.",
            result.Description);

        Assert.Equal(
            "English",
            result.Language);

        Assert.Equal(
            1956,
            result.PublicationYear);

        Assert.Equal(
            catalog.Publisher.PublisherId,
            result.Publisher.PublisherId);

        Assert.Equal(
            "Integration Test Publisher",
            result.Publisher.Name);

        BookAuthorResponse author =
            Assert.Single(result.Authors);

        Assert.Equal(
            catalog.Author.AuthorId,
            author.AuthorId);

        Assert.Equal(
            "Integration Test Author",
            author.Name);

        BookCategoryResponse category =
            Assert.Single(result.Categories);

        Assert.Equal(
            catalog.Category.CategoryId,
            category.CategoryId);

        Assert.Equal(
            "Integration Test Category",
            category.Name);

        BookImageResponse image =
            Assert.Single(result.Images);

        Assert.True(image.IsMain);

        Assert.True(
        Uri.TryCreate(
            image.ImageUrl,
            UriKind.Absolute,
            out Uri? imageUri));

            Assert.NotNull(imageUri);

            Assert.Equal(
                _client.BaseAddress!.Host,
                imageUri.Host);

            Assert.Contains(
                $"/uploads/books/{result.BookId}/",
                imageUri.AbsolutePath);

        Assert.EndsWith(
            ".jpg",
            image.ImageUrl,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            0,
            result.AvailableListingsCount);

        Assert.NotNull(
            response.Headers.Location);

        Assert.EndsWith(
            $"/api/books/{result.BookId}",
            response.Headers.Location.ToString());

        HttpResponseMessage imageResponse =
            await _client.GetAsync(
                image.ImageUrl);

        await AssertStatusCodeAsync(
            imageResponse,
            HttpStatusCode.OK);

        Assert.Equal(
            "image/jpeg",
            imageResponse.Content.Headers
                .ContentType?
                .MediaType);

        using IServiceScope scope =
            _factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        Book savedBook =
            await dbContext.Books
                .AsNoTracking()
                .Include(book =>
                    book.BookAuthors)
                .Include(book =>
                    book.BookCategories)
                .Include(book =>
                    book.BookImages)
                .SingleAsync(
                    book =>
                        book.BookId ==
                        result.BookId);

        Assert.Equal(
            "The Cairo Trilogy",
            savedBook.Title);

        Assert.Equal(
            "9780307947109",
            savedBook.Isbn);

        Assert.Equal(
            catalog.Publisher.PublisherId,
            savedBook.PublisherId);

        Assert.False(savedBook.IsDeleted);

        Assert.False(
            string.IsNullOrWhiteSpace(
                savedBook.CreatedById));

        BookAuthor savedAuthor =
            Assert.Single(
                savedBook.BookAuthors);

        Assert.Equal(
            catalog.Author.AuthorId,
            savedAuthor.AuthorId);

        BookCategory savedCategory =
            Assert.Single(
                savedBook.BookCategories);

        Assert.Equal(
            catalog.Category.CategoryId,
            savedCategory.CategoryId);

        BookImage savedImage =
            Assert.Single(
                savedBook.BookImages);

        Assert.True(savedImage.IsMain);

        Assert.Equal(
            imageUri.AbsolutePath,
            savedImage.ImageUrl);
    }

    [Fact]
    public async Task CreateBook_AsAdmin_WithoutImages_ReturnsBadRequest()
    {
        await ClearBookCatalogAsync();

        var catalog =
            await SeedCatalogAsync();

        await AuthenticateAsAdminAsync();

        CreateBookRequest request =
            CreateRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        using MultipartFormDataContent content =
            CreateBookMultipartContent(
                request,
                imageCount: 0,
                mainImageIndex: null);

        HttpResponseMessage response =
            await _client.PostAsync(
                "/api/books",
                content);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.BadRequest);

        Assert.Equal(
            0,
            await GetBooksCountAsync());
    }

    [Fact]
    public async Task CreateBook_AsAdmin_WithInvalidImageType_ReturnsBadRequest()
    {
        await ClearBookCatalogAsync();

        var catalog =
            await SeedCatalogAsync();

        await AuthenticateAsAdminAsync();

        CreateBookRequest request =
            CreateRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        using MultipartFormDataContent content =
            CreateBookMultipartContent(
                request,
                imageCount: 1,
                mainImageIndex: 0,
                extension: ".txt",
                contentType: "text/plain");

        HttpResponseMessage response =
            await _client.PostAsync(
                "/api/books",
                content);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.BadRequest);

        Assert.Equal(
            0,
            await GetBooksCountAsync());
    }

    [Fact]
    public async Task CreateBook_AsAdmin_WithDuplicateIsbn_ReturnsConflict()
    {
        await ClearBookCatalogAsync();

        var catalog =
            await SeedCatalogAsync();

        await AuthenticateAsAdminAsync();

        CreateBookRequest firstRequest =
            CreateRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        using MultipartFormDataContent firstContent =
            CreateBookMultipartContent(
                firstRequest,
                fileNamePrefix: "first");

        HttpResponseMessage firstResponse =
            await _client.PostAsync(
                "/api/books",
                firstContent);

        await AssertStatusCodeAsync(
            firstResponse,
            HttpStatusCode.Created);

        CreateBookRequest duplicateRequest =
            CreateRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        duplicateRequest.Title =
            "Another Book";

        duplicateRequest.Isbn =
            "978 0 307 94710 9";

        using MultipartFormDataContent duplicateContent =
            CreateBookMultipartContent(
                duplicateRequest,
                fileNamePrefix: "duplicate");

        HttpResponseMessage duplicateResponse =
            await _client.PostAsync(
                "/api/books",
                duplicateContent);

        await AssertStatusCodeAsync(
            duplicateResponse,
            HttpStatusCode.Conflict);

        Assert.Equal(
            1,
            await GetBooksCountAsync());
    }

    [Fact]
    public async Task CreateBook_AsAdmin_WithMissingPublisher_ReturnsNotFound()
    {
        await ClearBookCatalogAsync();

        var catalog =
            await SeedCatalogAsync();

        await AuthenticateAsAdminAsync();

        CreateBookRequest request =
            CreateRequest(
                publisherId: 999999,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        using MultipartFormDataContent content =
            CreateBookMultipartContent(request);

        HttpResponseMessage response =
            await _client.PostAsync(
                "/api/books",
                content);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.NotFound);

        Assert.Equal(
            0,
            await GetBooksCountAsync());
    }

    [Fact]
    public async Task CreateBook_AsAdmin_WithMissingAuthor_ReturnsNotFound()
    {
        await ClearBookCatalogAsync();

        var catalog =
            await SeedCatalogAsync();

        await AuthenticateAsAdminAsync();

        CreateBookRequest request =
            CreateRequest(
                catalog.Publisher.PublisherId,
                authorId: 999999,
                catalog.Category.CategoryId);

        using MultipartFormDataContent content =
            CreateBookMultipartContent(request);

        HttpResponseMessage response =
            await _client.PostAsync(
                "/api/books",
                content);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.NotFound);

        Assert.Equal(
            0,
            await GetBooksCountAsync());
    }

    [Fact]
    public async Task CreateBook_AsAdmin_WithMissingCategory_ReturnsNotFound()
    {
        await ClearBookCatalogAsync();

        var catalog =
            await SeedCatalogAsync();

        await AuthenticateAsAdminAsync();

        CreateBookRequest request =
            CreateRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                categoryId: 999999);

        using MultipartFormDataContent content =
            CreateBookMultipartContent(request);

        HttpResponseMessage response =
            await _client.PostAsync(
                "/api/books",
                content);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.NotFound);

        Assert.Equal(
            0,
            await GetBooksCountAsync());
    }

    [Fact]
    public async Task UpdateBook_AsAdmin_UpdatesRelationshipsAndAddsImages()
    {
        await ClearBookCatalogAsync();

        var originalCatalog =
            await SeedCatalogAsync();

        await AuthenticateAsAdminAsync();

        CreateBookRequest createRequest =
            CreateRequest(
                originalCatalog.Publisher.PublisherId,
                originalCatalog.Author.AuthorId,
                originalCatalog.Category.CategoryId);

        using MultipartFormDataContent createContent =
            CreateBookMultipartContent(
                createRequest,
                imageCount: 1,
                mainImageIndex: 0,
                fileNamePrefix: "original");

        HttpResponseMessage createResponse =
            await _client.PostAsync(
                "/api/books",
                createContent);

        await AssertStatusCodeAsync(
            createResponse,
            HttpStatusCode.Created);

        BookDetailsResponse? createdBook =
            await createResponse.Content
                .ReadFromJsonAsync<BookDetailsResponse>();

        Assert.NotNull(createdBook);

        string originalImageUrl =
            Assert.Single(
                createdBook.Images)
                .ImageUrl;

        var updatedCatalog =
            await SeedAdditionalCatalogAsync();

        UpdateBookRequest updateRequest =
            CreateUpdateRequest(
                updatedCatalog.Publisher.PublisherId,
                updatedCatalog.Author.AuthorId,
                updatedCatalog.Category.CategoryId);

        using MultipartFormDataContent updateContent =
            CreateBookMultipartContent(
                updateRequest,
                imageCount: 2,
                mainImageIndex: 0,
                fileNamePrefix: "updated");

        HttpResponseMessage updateResponse =
            await _client.PutAsync(
                $"/api/books/{createdBook.BookId}",
                updateContent);

        await AssertStatusCodeAsync(
            updateResponse,
            HttpStatusCode.OK);

        BookDetailsResponse? result =
            await updateResponse.Content
                .ReadFromJsonAsync<BookDetailsResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            "Updated Book Title",
            result.Title);

        Assert.Equal(
            "0306406152",
            result.Isbn);

        Assert.Equal(
            "Updated book description.",
            result.Description);

        Assert.Equal(
            "Arabic",
            result.Language);

        Assert.Equal(
            1980,
            result.PublicationYear);

        Assert.Equal(
            updatedCatalog.Publisher.PublisherId,
            result.Publisher.PublisherId);

        BookAuthorResponse responseAuthor =
            Assert.Single(result.Authors);

        Assert.Equal(
            updatedCatalog.Author.AuthorId,
            responseAuthor.AuthorId);

        BookCategoryResponse responseCategory =
            Assert.Single(result.Categories);

        Assert.Equal(
            updatedCatalog.Category.CategoryId,
            responseCategory.CategoryId);

        Assert.Equal(
            3,
            result.Images.Count);

        Assert.Single(
            result.Images,
            image => image.IsMain);

        Assert.Contains(
            result.Images,
            image =>
                image.ImageUrl ==
                originalImageUrl);

        using IServiceScope scope =
            _factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        Book updatedBook =
            await dbContext.Books
                .AsNoTracking()
                .Include(book =>
                    book.BookAuthors)
                .Include(book =>
                    book.BookCategories)
                .Include(book =>
                    book.BookImages)
                .SingleAsync(
                    book =>
                        book.BookId ==
                        createdBook.BookId);

        Assert.Equal(
            "Updated Book Title",
            updatedBook.Title);

        Assert.Equal(
            "0306406152",
            updatedBook.Isbn);

        Assert.Equal(
            updatedCatalog.Publisher.PublisherId,
            updatedBook.PublisherId);

        Assert.NotNull(
            updatedBook.UpdatedAt);

        Assert.False(
            string.IsNullOrWhiteSpace(
                updatedBook.UpdatedById));

        BookAuthor savedAuthor =
            Assert.Single(
                updatedBook.BookAuthors);

        Assert.Equal(
            updatedCatalog.Author.AuthorId,
            savedAuthor.AuthorId);

        Assert.DoesNotContain(
            updatedBook.BookAuthors,
            relationship =>
                relationship.AuthorId ==
                originalCatalog.Author.AuthorId);

        BookCategory savedCategory =
            Assert.Single(
                updatedBook.BookCategories);

        Assert.Equal(
            updatedCatalog.Category.CategoryId,
            savedCategory.CategoryId);

        Assert.DoesNotContain(
            updatedBook.BookCategories,
            relationship =>
                relationship.CategoryId ==
                originalCatalog.Category.CategoryId);

        Assert.Equal(
            3,
            updatedBook.BookImages.Count);

        Assert.Single(
            updatedBook.BookImages,
            image => image.IsMain);

        Uri originalImageUri =
     new Uri(
         originalImageUrl,
         UriKind.Absolute);

        Assert.Contains(
            updatedBook.BookImages,
            image =>
                image.ImageUrl ==
                originalImageUri.AbsolutePath);
    }

    [Fact]
    public async Task UpdateBook_AsCustomer_ReturnsForbidden()
    {
        await ClearBookCatalogAsync();

        await AuthenticateAsync(
            ApplicationRoles.Customer);

        UpdateBookRequest request =
            CreateUpdateRequest(
                publisherId: 1,
                authorId: 1,
                categoryId: 1);

        using MultipartFormDataContent content =
            CreateBookMultipartContent(
                request,
                imageCount: 0,
                mainImageIndex: null);

        HttpResponseMessage response =
            await _client.PutAsync(
                "/api/books/999999",
                content);

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ManageBookImages_AsAdmin_UploadsSetsMainAndDeletesImage()
    {
        await ClearBookCatalogAsync();

        var catalog =
            await SeedCatalogAsync();

        await AuthenticateAsAdminAsync();

        CreateBookRequest createRequest =
            CreateRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        using MultipartFormDataContent createContent =
            CreateBookMultipartContent(
                createRequest,
                imageCount: 1,
                mainImageIndex: 0,
                fileNamePrefix: "initial");

        HttpResponseMessage createResponse =
            await _client.PostAsync(
                "/api/books",
                createContent);

        await AssertStatusCodeAsync(
            createResponse,
            HttpStatusCode.Created);

        BookDetailsResponse? createdBook =
            await createResponse.Content
                .ReadFromJsonAsync<BookDetailsResponse>();

        Assert.NotNull(createdBook);

        using MultipartFormDataContent uploadContent =
            CreateImagesMultipartContent(
                imageCount: 2,
                mainImageIndex: 1);

        HttpResponseMessage uploadResponse =
            await _client.PostAsync(
                $"/api/books/{createdBook.BookId}/images",
                uploadContent);

        await AssertStatusCodeAsync(
            uploadResponse,
            HttpStatusCode.OK);

        UploadBookImagesResponse? uploadedImages =
            await uploadResponse.Content
                .ReadFromJsonAsync<UploadBookImagesResponse>();

        Assert.NotNull(uploadedImages);

        Assert.Equal(
            3,
            uploadedImages.Images.Count);

        BookImageResponse currentMainImage =
            Assert.Single(
                uploadedImages.Images,
                image => image.IsMain);

        Assert.EndsWith(
            ".png",
            currentMainImage.ImageUrl,
            StringComparison.OrdinalIgnoreCase);

        BookImageResponse imageToSetAsMain =
            uploadedImages.Images
                .First(image =>
                    image.BookImageId !=
                    currentMainImage.BookImageId);

        HttpResponseMessage setMainResponse =
            await _client.PutAsync(
                $"/api/books/{createdBook.BookId}" +
                $"/images/{imageToSetAsMain.BookImageId}/main",
                content: null);

        await AssertStatusCodeAsync(
            setMainResponse,
            HttpStatusCode.OK);

        UploadBookImagesResponse? setMainResult =
            await setMainResponse.Content
                .ReadFromJsonAsync<UploadBookImagesResponse>();

        Assert.NotNull(setMainResult);

        BookImageResponse changedMainImage =
            Assert.Single(
                setMainResult.Images,
                image => image.IsMain);

        Assert.Equal(
            imageToSetAsMain.BookImageId,
            changedMainImage.BookImageId);

        string deletedImageUrl =
            changedMainImage.ImageUrl;

        HttpResponseMessage deleteImageResponse =
            await _client.DeleteAsync(
                $"/api/books/{createdBook.BookId}" +
                $"/images/{changedMainImage.BookImageId}");

        await AssertStatusCodeAsync(
            deleteImageResponse,
            HttpStatusCode.OK);

        UploadBookImagesResponse? deleteResult =
            await deleteImageResponse.Content
                .ReadFromJsonAsync<UploadBookImagesResponse>();

        Assert.NotNull(deleteResult);

        Assert.Equal(
            2,
            deleteResult.Images.Count);

        Assert.DoesNotContain(
            deleteResult.Images,
            image =>
                image.BookImageId ==
                changedMainImage.BookImageId);

        Assert.Single(
            deleteResult.Images,
            image => image.IsMain);

        HttpResponseMessage deletedFileResponse =
            await _client.GetAsync(
                deletedImageUrl);

        await AssertStatusCodeAsync(
            deletedFileResponse,
            HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteBook_WithoutToken_ReturnsUnauthorized()
    {
        await ClearBookCatalogAsync();

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        HttpResponseMessage response =
            await _client.DeleteAsync(
                "/api/books/999999");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteBook_AsAdmin_PerformsSoftDelete()
    {
        await ClearBookCatalogAsync();

        var catalog =
            await SeedCatalogAsync();

        await AuthenticateAsAdminAsync();

        CreateBookRequest createRequest =
            CreateRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        using MultipartFormDataContent content =
            CreateBookMultipartContent(createRequest);

        HttpResponseMessage createResponse =
            await _client.PostAsync(
                "/api/books",
                content);

        await AssertStatusCodeAsync(
            createResponse,
            HttpStatusCode.Created);

        BookDetailsResponse? createdBook =
            await createResponse.Content
                .ReadFromJsonAsync<BookDetailsResponse>();

        Assert.NotNull(createdBook);

        HttpResponseMessage deleteResponse =
            await _client.DeleteAsync(
                $"/api/books/{createdBook.BookId}");

        await AssertStatusCodeAsync(
            deleteResponse,
            HttpStatusCode.OK);

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        HttpResponseMessage getDeletedResponse =
            await _client.GetAsync(
                $"/api/books/{createdBook.BookId}");

        await AssertStatusCodeAsync(
            getDeletedResponse,
            HttpStatusCode.NotFound);

        HttpResponseMessage getBooksResponse =
            await _client.GetAsync("/api/books");

        await AssertStatusCodeAsync(
            getBooksResponse,
            HttpStatusCode.OK);

        PagedResponse<BookResponse>? result =
    await getBooksResponse.Content
        .ReadFromJsonAsync<
            PagedResponse<BookResponse>>();

        Assert.NotNull(result);
        Assert.NotNull(result.Items);;

        using IServiceScope scope =
            _factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        Book deletedBook =
            await dbContext.Books
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Include(book =>
                    book.BookAuthors)
                .Include(book =>
                    book.BookCategories)
                .Include(book =>
                    book.BookImages)
                .SingleAsync(
                    book =>
                        book.BookId ==
                        createdBook.BookId);

        Assert.True(deletedBook.IsDeleted);

        Assert.NotNull(
            deletedBook.DeletedAt);

        Assert.False(
            string.IsNullOrWhiteSpace(
                deletedBook.DeletedById));

        Assert.Single(
            deletedBook.BookAuthors);

        Assert.Single(
            deletedBook.BookCategories);

        Assert.Single(
            deletedBook.BookImages);
    }

    [Fact]
    public async Task GetBooks_WithSearchAndPagination_ReturnsPagedResponse()
    {
        await ClearBookCatalogAsync();

        var catalog =
            await SeedCatalogAsync();

        await AuthenticateAsAdminAsync();

        CreateBookRequest alphaRequest =
            CreateRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        alphaRequest.Title =
            "Alpha Book";

        alphaRequest.Isbn =
            "9780131101784";

        using MultipartFormDataContent alphaContent =
            CreateBookMultipartContent(
                alphaRequest,
                imageCount: 1,
                mainImageIndex: 0,
                fileNamePrefix: "alpha-search");

        HttpResponseMessage alphaResponse =
            await _client.PostAsync(
                "/api/books",
                alphaContent);

        await AssertStatusCodeAsync(
            alphaResponse,
            HttpStatusCode.Created);

        CreateBookRequest betaRequest =
            CreateRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        betaRequest.Title =
            "Beta Book";

        betaRequest.Isbn =
            "9780131101791";

        using MultipartFormDataContent betaContent =
            CreateBookMultipartContent(
                betaRequest,
                imageCount: 1,
                mainImageIndex: 0,
                fileNamePrefix: "beta-search");

        HttpResponseMessage betaResponse =
            await _client.PostAsync(
                "/api/books",
                betaContent);

        await AssertStatusCodeAsync(
            betaResponse,
            HttpStatusCode.Created);

        CreateBookRequest gammaRequest =
            CreateRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        gammaRequest.Title =
            "Gamma Book";

        gammaRequest.Isbn =
            "9780131101807";

        using MultipartFormDataContent gammaContent =
            CreateBookMultipartContent(
                gammaRequest,
                imageCount: 1,
                mainImageIndex: 0,
                fileNamePrefix: "gamma-search");

        HttpResponseMessage gammaResponse =
            await _client.PostAsync(
                "/api/books",
                gammaContent);

        await AssertStatusCodeAsync(
            gammaResponse,
            HttpStatusCode.Created);

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        HttpResponseMessage response =
            await _client.GetAsync(
                "/api/books" +
                "?searchTerm=book" +
                "&sortBy=title" +
                "&sortDirection=asc" +
                "&pageNumber=2" +
                "&pageSize=2");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.OK);

        PagedResponse<BookResponse>? result =
            await response.Content
                .ReadFromJsonAsync<
                    PagedResponse<BookResponse>>();

        Assert.NotNull(result);

        BookResponse book =
            Assert.Single(result.Items);

        Assert.Equal(
            "Gamma Book",
            book.Title);

        Assert.Equal(
            2,
            result.PageNumber);

        Assert.Equal(
            2,
            result.PageSize);

        Assert.Equal(
            3,
            result.TotalCount);

        Assert.Equal(
            2,
            result.TotalPages);

        Assert.True(
            result.HasPreviousPage);

        Assert.False(
            result.HasNextPage);

        Assert.NotNull(
            book.MainImageUrl);

        Assert.True(
            Uri.TryCreate(
                book.MainImageUrl,
                UriKind.Absolute,
                out Uri? imageUri));

        Assert.NotNull(imageUri);

        Assert.Equal(
            _client.BaseAddress!.Host,
            imageUri.Host);
    }
    [Theory]
    [InlineData("pageNumber=0")]
    [InlineData("pageSize=51")]
    [InlineData("minPrice=-1")]
    [InlineData("maxPrice=-1")]
    [InlineData("minPrice=100&maxPrice=10")]
    [InlineData("sortBy=invalid")]
    [InlineData("sortDirection=random")]
    [InlineData("format=999")]
    [InlineData("condition=999")]
    public async Task GetBooks_WithInvalidQuery_ReturnsBadRequest(
    string queryString)
    {
        await ClearBookCatalogAsync();

        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        HttpResponseMessage response =
            await _client.GetAsync(
                $"/api/books?{queryString}");

        await AssertStatusCodeAsync(
            response,
            HttpStatusCode.BadRequest);
    }


    private async Task AuthenticateAsAdminAsync()
    {
        await AuthenticateAsync(
            ApplicationRoles.Admin);
    }

    private async Task AuthenticateAsync(
        string role)
    {
        string token =
            await IntegrationTestAuthenticationHelper
                .CreateAccessTokenAsync(
                    _factory,
                    role);

        IntegrationTestAuthenticationHelper
            .SetBearerToken(
                _client,
                token);
    }

    private async Task<(
        Publisher Publisher,
        Author Author,
        Category Category)> SeedCatalogAsync()
    {
        using IServiceScope scope =
            _factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var publisher = new Publisher
        {
            Name = "Integration Test Publisher",
            Website =
                "https://publisher.example.com"
        };

        var author = new Author
        {
            Name = "Integration Test Author",
            Biography =
                "Author created for book integration tests."
        };

        var category = new Category
        {
            Name = "Integration Test Category",
            Description =
                "Category created for book integration tests."
        };

        dbContext.AddRange(
            publisher,
            author,
            category);

        await dbContext.SaveChangesAsync();

        return (
            publisher,
            author,
            category);
    }

    private async Task<(
        Publisher Publisher,
        Author Author,
        Category Category)> SeedAdditionalCatalogAsync()
    {
        using IServiceScope scope =
            _factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var publisher = new Publisher
        {
            Name = "Updated Integration Publisher",
            Website =
                "https://updated-publisher.example.com"
        };

        var author = new Author
        {
            Name = "Updated Integration Author",
            Biography =
                "Updated author for book integration tests."
        };

        var category = new Category
        {
            Name = "Updated Integration Category",
            Description =
                "Updated category for book integration tests."
        };

        dbContext.AddRange(
            publisher,
            author,
            category);

        await dbContext.SaveChangesAsync();

        return (
            publisher,
            author,
            category);
    }

    private static CreateBookRequest CreateRequest(
        int publisherId,
        int authorId,
        int categoryId)
    {
        return new CreateBookRequest
        {
            Title =
                "  The Cairo Trilogy  ",

            Isbn =
                "978-0-307-94710-9",

            Description =
                "  A literary trilogy set in Cairo.  ",

            Language =
                "  English  ",

            PublicationYear = 1956,

            PublisherId = publisherId,

            AuthorIds =
            [
                authorId
            ],

            CategoryIds =
            [
                categoryId
            ]
        };
    }

    private static UpdateBookRequest CreateUpdateRequest(
        int publisherId,
        int authorId,
        int categoryId)
    {
        return new UpdateBookRequest
        {
            Title =
                "  Updated Book Title  ",

            Isbn =
                "0-306-40615-2",

            Description =
                "  Updated book description.  ",

            Language =
                "  Arabic  ",

            PublicationYear = 1980,

            PublisherId = publisherId,

            AuthorIds =
            [
                authorId
            ],

            CategoryIds =
            [
                categoryId
            ]
        };
    }

    private static MultipartFormDataContent
        CreateBookMultipartContent(
            CreateBookRequest request,
            int imageCount = 1,
            int? mainImageIndex = 0,
            string extension = ".jpg",
            string contentType = "image/jpeg",
            string fileNamePrefix = "book")
    {
        var content =
            new MultipartFormDataContent();

        AddBookFields(
            content,
            request.Title,
            request.Isbn,
            request.Description,
            request.Language,
            request.PublicationYear,
            request.PublisherId,
            request.AuthorIds,
            request.CategoryIds);

        AddImages(
            content,
            imageCount,
            mainImageIndex,
            extension,
            contentType,
            fileNamePrefix);

        return content;
    }

    private static MultipartFormDataContent
        CreateBookMultipartContent(
            UpdateBookRequest request,
            int imageCount = 0,
            int? mainImageIndex = null,
            string extension = ".jpg",
            string contentType = "image/jpeg",
            string fileNamePrefix = "updated")
    {
        var content =
            new MultipartFormDataContent();

        AddBookFields(
            content,
            request.Title,
            request.Isbn,
            request.Description,
            request.Language,
            request.PublicationYear,
            request.PublisherId,
            request.AuthorIds,
            request.CategoryIds);

        AddImages(
            content,
            imageCount,
            mainImageIndex,
            extension,
            contentType,
            fileNamePrefix);

        return content;
    }

    private static MultipartFormDataContent
        CreateImagesMultipartContent(
            int imageCount,
            int? mainImageIndex)
    {
        var content =
            new MultipartFormDataContent();

        for (int index = 0;
             index < imageCount;
             index++)
        {
            string extension =
                index % 2 == 0
                    ? ".jpg"
                    : ".png";

            string contentType =
                extension == ".jpg"
                    ? "image/jpeg"
                    : "image/png";

            AddImage(
                content,
                fieldName: "Images",
                fileName:
                    $"additional-{index + 1}{extension}",
                contentType);
        }

        if (mainImageIndex.HasValue)
        {
            AddText(
                content,
                "MainImageIndex",
                mainImageIndex.Value.ToString());
        }

        return content;
    }

    private static void AddBookFields(
        MultipartFormDataContent content,
        string title,
        string? isbn,
        string? description,
        string language,
        int? publicationYear,
        int publisherId,
        IEnumerable<int> authorIds,
        IEnumerable<int> categoryIds)
    {
        AddText(
            content,
            "Title",
            title);

        AddOptionalText(
            content,
            "Isbn",
            isbn);

        AddOptionalText(
            content,
            "Description",
            description);

        AddText(
            content,
            "Language",
            language);

        if (publicationYear.HasValue)
        {
            AddText(
                content,
                "PublicationYear",
                publicationYear.Value.ToString());
        }

        AddText(
            content,
            "PublisherId",
            publisherId.ToString());

        foreach (int authorId in authorIds)
        {
            AddText(
                content,
                "AuthorIds",
                authorId.ToString());
        }

        foreach (int categoryId in categoryIds)
        {
            AddText(
                content,
                "CategoryIds",
                categoryId.ToString());
        }
    }

    private static void AddImages(
        MultipartFormDataContent content,
        int imageCount,
        int? mainImageIndex,
        string extension,
        string contentType,
        string fileNamePrefix)
    {
        for (int index = 0;
             index < imageCount;
             index++)
        {
            AddImage(
                content,
                fieldName: "images",
                fileName:
                    $"{fileNamePrefix}-{index + 1}{extension}",
                contentType);
        }

        if (mainImageIndex.HasValue)
        {
            AddText(
                content,
                "mainImageIndex",
                mainImageIndex.Value.ToString());
        }
    }

    private static void AddText(
        MultipartFormDataContent content,
        string name,
        string value)
    {
        content.Add(
            new StringContent(value),
            name);
    }

    private static void AddOptionalText(
        MultipartFormDataContent content,
        string name,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        AddText(
            content,
            name,
            value);
    }

    private static void AddImage(
        MultipartFormDataContent content,
        string fieldName,
        string fileName,
        string contentType)
    {
        byte[] fileBytes =
            Enumerable
                .Repeat((byte)1, 128)
                .ToArray();

        var fileContent =
            new ByteArrayContent(fileBytes);

        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue(
                contentType);

        content.Add(
            fileContent,
            fieldName,
            fileName);
    }

    private async Task<int> GetBooksCountAsync()
    {
        using IServiceScope scope =
            _factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        return await dbContext.Books
            .IgnoreQueryFilters()
            .CountAsync();
    }

    private async Task ClearBookCatalogAsync()
    {
        IntegrationTestAuthenticationHelper
            .ClearBearerToken(_client);

        using IServiceScope scope =
            _factory.Services.CreateScope();

        ApplicationDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        await dbContext.CartItems
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync();

        await dbContext.OrderItems
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync();

        await dbContext.Reviews
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync();

        await dbContext.Listings
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync();

        await dbContext.BookImages
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync();

        await dbContext.BookAuthors
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync();

        await dbContext.BookCategories
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync();

        await dbContext.Books
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync();

        await dbContext.Authors
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync();

        await dbContext.Categories
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync();

        await dbContext.Publishers
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync();

        IWebHostEnvironment environment =
            scope.ServiceProvider
                .GetRequiredService<IWebHostEnvironment>();

        string webRootPath =
            string.IsNullOrWhiteSpace(
                environment.WebRootPath)
                ? Path.Combine(
                    environment.ContentRootPath,
                    "wwwroot")
                : environment.WebRootPath;

        string booksFolder =
            Path.Combine(
                webRootPath,
                "uploads",
                "books");

        if (Directory.Exists(booksFolder))
        {
            Directory.Delete(
                booksFolder,
                recursive: true);
        }

        Directory.CreateDirectory(
            booksFolder);
    }

    private static async Task AssertStatusCodeAsync(
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

        Assert.True(
            response.StatusCode ==
            expectedStatusCode,
            $"Expected {expectedStatusCode}, " +
            $"but received {response.StatusCode}. " +
            $"Response: {responseBody}");
    }
}