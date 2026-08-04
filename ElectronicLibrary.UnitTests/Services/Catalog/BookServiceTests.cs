using ElectronicLibrary.BLL.Exceptions;
using ElectronicLibrary.BLL.Models.Storage;
using ElectronicLibrary.DAL.DTOs.Requests.Books;
using ElectronicLibrary.DAL.DTOs.Responses.Books;
using ElectronicLibrary.DAL.DTOs.Responses.Common;
using ElectronicLibrary.DAL.Enums;
using ElectronicLibrary.DAL.Models.Identity;
using ElectronicLibrary.DAL.Models.Marketplace;
using ElectronicLibrary.DAL.Models.Catalog;
using ElectronicLibrary.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ElectronicLibrary.UnitTests.Services.Catalog;

public class BookServiceTests
{
    [Fact]
    public async Task CreateBookAsync_WithValidRequest_CreatesBookWithRelationshipsAndImages()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        CreateBookRequest request =
            CreateValidRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        BookDetailsResponse response =
            await testContext.BookService.CreateBookAsync(
                request,
                [
                    testContext.CreateImageFile()
                ],
                mainImageIndex: 0);

        Book savedBook =
            await testContext.DbContext.Books
                .Include(book => book.BookAuthors)
                .Include(book => book.BookCategories)
                .Include(book => book.BookImages)
                .SingleAsync();

        Assert.True(response.BookId > 0);

        Assert.Equal(
            "The Cairo Trilogy",
            response.Title);

        Assert.Equal(
            "9780307947109",
            response.Isbn);

        Assert.Equal(
            "A literary trilogy set in Cairo.",
            response.Description);

        Assert.Equal(
            "English",
            response.Language);

        Assert.Equal(
            1956,
            response.PublicationYear);

        Assert.Equal(
            catalog.Publisher.PublisherId,
            response.Publisher.PublisherId);

        BookAuthorResponseAssertions(
            response,
            catalog.Author);

        BookCategoryResponseAssertions(
            response,
            catalog.Category);

        BookImageResponse responseImage =
            Assert.Single(response.Images);

        Assert.StartsWith(
            $"/uploads/books/{response.BookId}/",
            responseImage.ImageUrl);

        Assert.StartsWith(
            $"/uploads/books/{response.BookId}/",
            responseImage.ImageUrl);

        Assert.EndsWith(
             ".jpg",
              responseImage.ImageUrl,
              StringComparison.OrdinalIgnoreCase);

        Assert.True(responseImage.IsMain);

        Assert.Equal(
            0,
            response.AvailableListingsCount);

        Assert.Equal(
            "The Cairo Trilogy",
            savedBook.Title);

        Assert.Equal(
            "9780307947109",
            savedBook.Isbn);

        Assert.Equal(
            "A literary trilogy set in Cairo.",
            savedBook.Description);

        Assert.Equal(
            "unit-test-admin-id",
            savedBook.CreatedById);

        Assert.NotEqual(
            default(DateTime),
            savedBook.CreatedAt);

        Assert.False(savedBook.IsDeleted);

        BookAuthor savedBookAuthor =
            Assert.Single(savedBook.BookAuthors);

        Assert.Equal(
            catalog.Author.AuthorId,
            savedBookAuthor.AuthorId);

        BookCategory savedBookCategory =
            Assert.Single(savedBook.BookCategories);

        Assert.Equal(
            catalog.Category.CategoryId,
            savedBookCategory.CategoryId);

        BookImage savedImage =
            Assert.Single(savedBook.BookImages);

        Assert.Equal(
            responseImage.ImageUrl,
            savedImage.ImageUrl);

        Assert.True(savedImage.IsMain);

        Assert.Single(
            testContext.FileStorageService.StoredFiles);

        Assert.True(
            testContext.FileStorageService.StoredFiles
                .ContainsKey(
                    responseImage.ImageUrl.TrimStart('/')));
    }

    [Fact]
    public async Task CreateBookAsync_WithDuplicateIsbn_ThrowsConflictException()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        CreateBookRequest firstRequest =
            CreateValidRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        await testContext.BookService.CreateBookAsync(
            firstRequest,
            [
                testContext.CreateImageFile(
                    "first-book.jpg")
            ],
            mainImageIndex: 0);

        CreateBookRequest duplicateRequest =
            CreateValidRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        duplicateRequest.Title =
            "Another Book";

        duplicateRequest.Isbn =
            "978 0 307 94710 9";

        ConflictException exception =
            await Assert.ThrowsAsync<ConflictException>(
                () =>
                    testContext.BookService.CreateBookAsync(
                        duplicateRequest,
                        [
                            testContext.CreateImageFile(
                                "second-book.jpg")
                        ],
                        mainImageIndex: 0));

        Assert.Equal(
            "BookIsbnAlreadyExists",
            exception.Message);

        Assert.Equal(
            1,
            await testContext.DbContext.Books.CountAsync());

        Assert.Single(
            testContext.FileStorageService.StoredFiles);
    }

    [Theory]
    [InlineData("123456789")]
    [InlineData("123456789012")]
    [InlineData("978030794710A")]
    [InlineData("ISBN-9780307947109")]
    public async Task CreateBookAsync_WithInvalidIsbn_ThrowsInvalidOperationException(
        string invalidIsbn)
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        CreateBookRequest request =
            CreateValidRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        request.Isbn = invalidIsbn;

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    testContext.BookService.CreateBookAsync(
                        request,
                        [
                            testContext.CreateImageFile()
                        ],
                        mainImageIndex: 0));

        Assert.Equal(
            "InvalidIsbn",
            exception.Message);

        Assert.Empty(
            await testContext.DbContext.Books
                .ToListAsync());

        Assert.Empty(
            testContext.FileStorageService.StoredFiles);
    }

    [Fact]
    public async Task CreateBookAsync_WithMissingPublisher_ThrowsKeyNotFoundException()
    {
        await using var testContext =
            new BookServiceTestContext();

        var author = new Author
        {
            Name = "Test Author"
        };

        var category = new Category
        {
            Name = "Test Category"
        };

        testContext.DbContext.AddRange(
            author,
            category);

        await testContext.DbContext.SaveChangesAsync();

        CreateBookRequest request =
            CreateValidRequest(
                publisherId: 999,
                author.AuthorId,
                category.CategoryId);

        KeyNotFoundException exception =
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () =>
                    testContext.BookService.CreateBookAsync(
                        request,
                        [
                            testContext.CreateImageFile()
                        ],
                        mainImageIndex: 0));

        Assert.Equal(
            "PublisherNotFound",
            exception.Message);

        Assert.Empty(
            await testContext.DbContext.Books
                .ToListAsync());

        Assert.Empty(
            testContext.FileStorageService.StoredFiles);
    }

    [Fact]
    public async Task CreateBookAsync_WithMissingAuthor_ThrowsKeyNotFoundException()
    {
        await using var testContext =
            new BookServiceTestContext();

        var publisher = new Publisher
        {
            Name = "Test Publisher"
        };

        var category = new Category
        {
            Name = "Test Category"
        };

        testContext.DbContext.AddRange(
            publisher,
            category);

        await testContext.DbContext.SaveChangesAsync();

        CreateBookRequest request =
            CreateValidRequest(
                publisher.PublisherId,
                authorId: 999,
                category.CategoryId);

        KeyNotFoundException exception =
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () =>
                    testContext.BookService.CreateBookAsync(
                        request,
                        [
                            testContext.CreateImageFile()
                        ],
                        mainImageIndex: 0));

        Assert.Equal(
            "OneOrMoreAuthorsNotFound",
            exception.Message);

        Assert.Empty(
            await testContext.DbContext.Books
                .ToListAsync());
    }

    [Fact]
    public async Task CreateBookAsync_WithMissingCategory_ThrowsKeyNotFoundException()
    {
        await using var testContext =
            new BookServiceTestContext();

        var publisher = new Publisher
        {
            Name = "Test Publisher"
        };

        var author = new Author
        {
            Name = "Test Author"
        };

        testContext.DbContext.AddRange(
            publisher,
            author);

        await testContext.DbContext.SaveChangesAsync();

        CreateBookRequest request =
            CreateValidRequest(
                publisher.PublisherId,
                author.AuthorId,
                categoryId: 999);

        KeyNotFoundException exception =
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () =>
                    testContext.BookService.CreateBookAsync(
                        request,
                        [
                            testContext.CreateImageFile()
                        ],
                        mainImageIndex: 0));

        Assert.Equal(
            "OneOrMoreCategoriesNotFound",
            exception.Message);

        Assert.Empty(
            await testContext.DbContext.Books
                .ToListAsync());
    }

    [Fact]
    public async Task CreateBookAsync_WithEmptyAuthorList_ThrowsInvalidOperationException()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        CreateBookRequest request =
            CreateValidRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        request.AuthorIds = [];

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    testContext.BookService.CreateBookAsync(
                        request,
                        [
                            testContext.CreateImageFile()
                        ],
                        mainImageIndex: 0));

        Assert.Equal(
            "BookAuthorsRequired",
            exception.Message);
    }

    [Fact]
    public async Task CreateBookAsync_WithEmptyCategoryList_ThrowsInvalidOperationException()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        CreateBookRequest request =
            CreateValidRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        request.CategoryIds = [];

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    testContext.BookService.CreateBookAsync(
                        request,
                        [
                            testContext.CreateImageFile()
                        ],
                        mainImageIndex: 0));

        Assert.Equal(
            "BookCategoriesRequired",
            exception.Message);
    }

    [Fact]
    public async Task CreateBookAsync_WithFuturePublicationYear_ThrowsInvalidOperationException()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        CreateBookRequest request =
            CreateValidRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        request.PublicationYear =
            DateTime.UtcNow.Year + 1;

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    testContext.BookService.CreateBookAsync(
                        request,
                        [
                            testContext.CreateImageFile()
                        ],
                        mainImageIndex: 0));

        Assert.Equal(
            "InvalidPublicationYear",
            exception.Message);
    }

    [Fact]
    public async Task CreateBookAsync_WithDuplicateAuthorIds_ThrowsInvalidOperationException()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        CreateBookRequest request =
            CreateValidRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        request.AuthorIds =
        [
            catalog.Author.AuthorId,
            catalog.Author.AuthorId
        ];

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    testContext.BookService.CreateBookAsync(
                        request,
                        [
                            testContext.CreateImageFile()
                        ],
                        mainImageIndex: 0));

        Assert.Equal(
            "DuplicateAuthorIds",
            exception.Message);
    }

    [Fact]
    public async Task CreateBookAsync_WithDuplicateCategoryIds_ThrowsInvalidOperationException()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        CreateBookRequest request =
            CreateValidRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        request.CategoryIds =
        [
            catalog.Category.CategoryId,
            catalog.Category.CategoryId
        ];

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    testContext.BookService.CreateBookAsync(
                        request,
                        [
                            testContext.CreateImageFile()
                        ],
                        mainImageIndex: 0));

        Assert.Equal(
            "DuplicateCategoryIds",
            exception.Message);
    }

    [Fact]
    public async Task CreateBookAsync_WithoutImages_ThrowsInvalidOperationException()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        CreateBookRequest request =
            CreateValidRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    testContext.BookService.CreateBookAsync(
                        request,
                        Array.Empty<FileUploadData>(),
                        mainImageIndex: null));

        Assert.Equal(
            "BookImagesRequired",
            exception.Message);

        Assert.Empty(
            await testContext.DbContext.Books
                .ToListAsync());

        Assert.Empty(
            testContext.FileStorageService.StoredFiles);
    }

    [Fact]
    public async Task CreateBookAsync_WithInvalidMainImageIndex_ThrowsInvalidOperationException()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        CreateBookRequest request =
            CreateValidRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    testContext.BookService.CreateBookAsync(
                        request,
                        [
                            testContext.CreateImageFile()
                        ],
                        mainImageIndex: 5));

        Assert.Equal(
            "InvalidMainImageIndex",
            exception.Message);

        Assert.Empty(
            await testContext.DbContext.Books
                .ToListAsync());
    }

    [Fact]
    public async Task CreateBookAsync_WithMoreThanTenImages_ThrowsInvalidOperationException()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        CreateBookRequest request =
            CreateValidRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        List<FileUploadData> images =
            Enumerable.Range(1, 11)
                .Select(index =>
                    testContext.CreateImageFile(
                        $"book-{index}.jpg"))
                .ToList();

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    testContext.BookService.CreateBookAsync(
                        request,
                        images,
                        mainImageIndex: 0));

        Assert.Equal(
            "MaximumBookImagesExceeded",
            exception.Message);

        Assert.Empty(
            await testContext.DbContext.Books
                .ToListAsync());
    }

    [Fact]
    public async Task UpdateBookAsync_WithValidRequest_UpdatesBookRelationshipsAndAddsImages()
    {
        await using var testContext =
            new BookServiceTestContext();

        var originalCatalog =
            await SeedCatalogAsync(testContext);

        BookDetailsResponse createdBook =
            await testContext.BookService.CreateBookAsync(
                CreateValidRequest(
                    originalCatalog.Publisher.PublisherId,
                    originalCatalog.Author.AuthorId,
                    originalCatalog.Category.CategoryId),
                [
                    testContext.CreateImageFile(
                        "original-cover.jpg")
                ],
                mainImageIndex: 0);

        string originalImageUrl =
            Assert.Single(createdBook.Images).ImageUrl;

        var newPublisher = new Publisher
        {
            Name = "Updated Publisher",
            Website =
                "https://publisher.example.com"
        };

        var newAuthor = new Author
        {
            Name = "Updated Author"
        };

        var newCategory = new Category
        {
            Name = "Updated Category"
        };

        testContext.DbContext.AddRange(
            newPublisher,
            newAuthor,
            newCategory);

        await testContext.DbContext.SaveChangesAsync();

        UpdateBookRequest updateRequest =
            CreateValidUpdateRequest(
                newPublisher.PublisherId,
                newAuthor.AuthorId,
                newCategory.CategoryId);

        BookDetailsResponse response =
            await testContext.BookService.UpdateBookAsync(
                createdBook.BookId,
                updateRequest,
                [
                    testContext.CreateImageFile(
                        "updated-main.jpg"),
                    testContext.CreateImageFile(
                        "updated-secondary.png",
                        "image/png")
                ],
                mainImageIndex: 0);

        testContext.DbContext.ChangeTracker.Clear();

        Book updatedBook =
            await testContext.DbContext.Books
                .Include(book => book.BookAuthors)
                .Include(book => book.BookCategories)
                .Include(book => book.BookImages)
                .SingleAsync(
                    book =>
                        book.BookId ==
                        createdBook.BookId);

        Assert.Equal(
            "Updated Book Title",
            response.Title);

        Assert.Equal(
            "0306406152",
            response.Isbn);

        Assert.Equal(
            "Updated description.",
            response.Description);

        Assert.Equal(
            "Arabic",
            response.Language);

        Assert.Equal(
            1980,
            response.PublicationYear);

        Assert.Equal(
            newPublisher.PublisherId,
            response.Publisher.PublisherId);

        BookAuthorResponse responseAuthor =
            Assert.Single(response.Authors);

        Assert.Equal(
            newAuthor.AuthorId,
            responseAuthor.AuthorId);

        BookCategoryResponse responseCategory =
            Assert.Single(response.Categories);

        Assert.Equal(
            newCategory.CategoryId,
            responseCategory.CategoryId);

        Assert.Equal(
            3,
            response.Images.Count);

        Assert.Single(
            response.Images,
            image => image.IsMain);

        Assert.Contains(
            response.Images,
            image =>
                image.ImageUrl ==
                originalImageUrl);

        Assert.Equal(
            "Updated Book Title",
            updatedBook.Title);

        Assert.Equal(
            "0306406152",
            updatedBook.Isbn);

        Assert.Equal(
            "Updated description.",
            updatedBook.Description);

        Assert.Equal(
            "Arabic",
            updatedBook.Language);

        Assert.Equal(
            newPublisher.PublisherId,
            updatedBook.PublisherId);

        Assert.NotNull(
            updatedBook.UpdatedAt);

        Assert.Equal(
            "unit-test-admin-id",
            updatedBook.UpdatedById);

        BookAuthor updatedAuthorRelationship =
            Assert.Single(updatedBook.BookAuthors);

        Assert.Equal(
            newAuthor.AuthorId,
            updatedAuthorRelationship.AuthorId);

        BookCategory updatedCategoryRelationship =
            Assert.Single(updatedBook.BookCategories);

        Assert.Equal(
            newCategory.CategoryId,
            updatedCategoryRelationship.CategoryId);

        Assert.Equal(
            3,
            updatedBook.BookImages.Count);

        Assert.Single(
            updatedBook.BookImages,
            image => image.IsMain);

        Assert.Contains(
            updatedBook.BookImages,
            image =>
                image.ImageUrl ==
                originalImageUrl);

        Assert.DoesNotContain(
            updatedBook.BookAuthors,
            relationship =>
                relationship.AuthorId ==
                originalCatalog.Author.AuthorId);

        Assert.DoesNotContain(
            updatedBook.BookCategories,
            relationship =>
                relationship.CategoryId ==
                originalCatalog.Category.CategoryId);

        Assert.Equal(
            3,
            testContext.FileStorageService.StoredFiles.Count);
    }

    [Fact]
    public async Task UpdateBookAsync_WithoutNewImages_KeepsExistingImages()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        BookDetailsResponse createdBook =
            await testContext.BookService.CreateBookAsync(
                CreateValidRequest(
                    catalog.Publisher.PublisherId,
                    catalog.Author.AuthorId,
                    catalog.Category.CategoryId),
                [
                    testContext.CreateImageFile()
                ],
                mainImageIndex: 0);

        string originalImageUrl =
            Assert.Single(createdBook.Images).ImageUrl;

        UpdateBookRequest updateRequest =
            CreateValidUpdateRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        BookDetailsResponse response =
            await testContext.BookService.UpdateBookAsync(
                createdBook.BookId,
                updateRequest,
                Array.Empty<FileUploadData>(),
                mainImageIndex: null);

        BookImageResponse responseImage =
            Assert.Single(response.Images);

        Assert.Equal(
            originalImageUrl,
            responseImage.ImageUrl);

        Assert.True(responseImage.IsMain);

        Assert.Single(
            testContext.FileStorageService.StoredFiles);
    }

    [Fact]
    public async Task UpdateBookAsync_WithUnchangedRelationships_DoesNotDuplicateRelationships()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        BookDetailsResponse createdBook =
            await testContext.BookService.CreateBookAsync(
                CreateValidRequest(
                    catalog.Publisher.PublisherId,
                    catalog.Author.AuthorId,
                    catalog.Category.CategoryId),
                [
                    testContext.CreateImageFile()
                ],
                mainImageIndex: 0);

        UpdateBookRequest updateRequest =
            CreateValidUpdateRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        await testContext.BookService.UpdateBookAsync(
            createdBook.BookId,
            updateRequest,
            Array.Empty<FileUploadData>(),
            mainImageIndex: null);

        testContext.DbContext.ChangeTracker.Clear();

        Book updatedBook =
            await testContext.DbContext.Books
                .Include(book => book.BookAuthors)
                .Include(book => book.BookCategories)
                .SingleAsync(
                    book =>
                        book.BookId ==
                        createdBook.BookId);

        Assert.Single(
            updatedBook.BookAuthors);

        Assert.Single(
            updatedBook.BookCategories);

        Assert.Equal(
            catalog.Author.AuthorId,
            updatedBook.BookAuthors
                .Single()
                .AuthorId);

        Assert.Equal(
            catalog.Category.CategoryId,
            updatedBook.BookCategories
                .Single()
                .CategoryId);
    }

    [Fact]
    public async Task UpdateBookAsync_WithDuplicateIsbn_ThrowsConflictException()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        CreateBookRequest firstRequest =
            CreateValidRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        BookDetailsResponse firstBook =
            await testContext.BookService.CreateBookAsync(
                firstRequest,
                [
                    testContext.CreateImageFile(
                        "first-book.jpg")
                ],
                mainImageIndex: 0);

        CreateBookRequest secondRequest =
            CreateValidRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        secondRequest.Title =
            "Second Book";

        secondRequest.Isbn =
            "0-306-40615-2";

        BookDetailsResponse secondBook =
            await testContext.BookService.CreateBookAsync(
                secondRequest,
                [
                    testContext.CreateImageFile(
                        "second-book.jpg")
                ],
                mainImageIndex: 0);

        UpdateBookRequest updateRequest =
            CreateValidUpdateRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        updateRequest.Title =
            "Second Book Updated";

        updateRequest.Isbn =
            firstBook.Isbn;

        ConflictException exception =
            await Assert.ThrowsAsync<ConflictException>(
                () =>
                    testContext.BookService.UpdateBookAsync(
                        secondBook.BookId,
                        updateRequest,
                        Array.Empty<FileUploadData>(),
                        mainImageIndex: null));

        Assert.Equal(
            "BookIsbnAlreadyExists",
            exception.Message);

        testContext.DbContext.ChangeTracker.Clear();

        Book unchangedBook =
            await testContext.DbContext.Books
                .SingleAsync(
                    book =>
                        book.BookId ==
                        secondBook.BookId);

        Assert.Equal(
            "Second Book",
            unchangedBook.Title);

        Assert.Equal(
            "0306406152",
            unchangedBook.Isbn);
    }

    [Fact]
    public async Task UpdateBookAsync_WithMissingBook_ThrowsKeyNotFoundException()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        UpdateBookRequest request =
            CreateValidUpdateRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        KeyNotFoundException exception =
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () =>
                    testContext.BookService.UpdateBookAsync(
                        bookId: 999,
                        request,
                        Array.Empty<FileUploadData>(),
                        mainImageIndex: null));

        Assert.Equal(
            "BookNotFound",
            exception.Message);
    }

    [Fact]
    public async Task GetBookByIdAsync_WithExistingBook_ReturnsBookDetails()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        BookDetailsResponse createdBook =
            await testContext.BookService.CreateBookAsync(
                CreateValidRequest(
                    catalog.Publisher.PublisherId,
                    catalog.Author.AuthorId,
                    catalog.Category.CategoryId),
                [
                    testContext.CreateImageFile()
                ],
                mainImageIndex: 0);

        BookDetailsResponse response =
            await testContext.BookService.GetBookByIdAsync(
                createdBook.BookId);

        Assert.Equal(
            createdBook.BookId,
            response.BookId);

        Assert.Equal(
            "The Cairo Trilogy",
            response.Title);

        Assert.Equal(
            "9780307947109",
            response.Isbn);

        Assert.Equal(
            catalog.Publisher.PublisherId,
            response.Publisher.PublisherId);

        Assert.Single(response.Authors);
        Assert.Single(response.Categories);
        Assert.Single(response.Images);

        Assert.Equal(
            0,
            response.AvailableListingsCount);
    }

    [Fact]
    public async Task GetBookByIdAsync_WithMissingBook_ThrowsKeyNotFoundException()
    {
        await using var testContext =
            new BookServiceTestContext();

        KeyNotFoundException exception =
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () =>
                    testContext.BookService.GetBookByIdAsync(
                        bookId: 999));

        Assert.Equal(
            "BookNotFound",
            exception.Message);
    }

    [Fact]
    public async Task UploadBookImagesAsync_WithValidFiles_AddsImagesAndChangesMainImage()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        BookDetailsResponse createdBook =
            await testContext.BookService.CreateBookAsync(
                CreateValidRequest(
                    catalog.Publisher.PublisherId,
                    catalog.Author.AuthorId,
                    catalog.Category.CategoryId),
                [
                    testContext.CreateImageFile(
                        "original.jpg")
                ],
                mainImageIndex: 0);

        UploadBookImagesResponse response =
            await testContext.BookService.UploadBookImagesAsync(
                createdBook.BookId,
                [
                    testContext.CreateImageFile(
                        "additional.png",
                        "image/png"),
                    testContext.CreateImageFile(
                        "new-main.webp",
                        "image/webp")
                ],
                mainImageIndex: 1);

        Assert.Equal(
            3,
            response.Images.Count);

        BookImageResponse mainImage =
        Assert.Single(
            response.Images,
            image => image.IsMain);

        Assert.EndsWith(
            ".webp",
            mainImage.ImageUrl,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            3,
            testContext.FileStorageService.StoredFiles.Count);
    }

    [Fact]
    public async Task SetMainBookImageAsync_WithExistingImage_ChangesMainImage()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        BookDetailsResponse createdBook =
            await testContext.BookService.CreateBookAsync(
                CreateValidRequest(
                    catalog.Publisher.PublisherId,
                    catalog.Author.AuthorId,
                    catalog.Category.CategoryId),
                [
                    testContext.CreateImageFile(
                        "first.jpg"),
                    testContext.CreateImageFile(
                        "second.png",
                        "image/png")
                ],
                mainImageIndex: 0);

        BookImageResponse secondImage =
            createdBook.Images.Single(
                image =>
                    !image.IsMain);

        UploadBookImagesResponse response =
            await testContext.BookService.SetMainBookImageAsync(
                createdBook.BookId,
                secondImage.BookImageId);

        BookImageResponse mainImage =
            Assert.Single(
                response.Images,
                image => image.IsMain);

        Assert.Equal(
            secondImage.BookImageId,
            mainImage.BookImageId);
    }

    [Fact]
    public async Task SetMainBookImageAsync_WithMissingImage_ThrowsKeyNotFoundException()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        BookDetailsResponse createdBook =
            await testContext.BookService.CreateBookAsync(
                CreateValidRequest(
                    catalog.Publisher.PublisherId,
                    catalog.Author.AuthorId,
                    catalog.Category.CategoryId),
                [
                    testContext.CreateImageFile()
                ],
                mainImageIndex: 0);

        KeyNotFoundException exception =
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () =>
                    testContext.BookService.SetMainBookImageAsync(
                        createdBook.BookId,
                        bookImageId: 999));

        Assert.Equal(
            "BookImageNotFound",
            exception.Message);
    }

    [Fact]
    public async Task DeleteBookImageAsync_WithMainImage_DeletesFileAndSelectsReplacement()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        BookDetailsResponse createdBook =
            await testContext.BookService.CreateBookAsync(
                CreateValidRequest(
                    catalog.Publisher.PublisherId,
                    catalog.Author.AuthorId,
                    catalog.Category.CategoryId),
                [
                    testContext.CreateImageFile(
                        "main.jpg"),
                    testContext.CreateImageFile(
                        "secondary.png",
                        "image/png")
                ],
                mainImageIndex: 0);

        BookImageResponse originalMainImage =
            createdBook.Images.Single(
                image => image.IsMain);

        UploadBookImagesResponse response =
            await testContext.BookService.DeleteBookImageAsync(
                createdBook.BookId,
                originalMainImage.BookImageId);

        BookImageResponse remainingImage =
            Assert.Single(response.Images);

        Assert.True(
            remainingImage.IsMain);

        Assert.DoesNotContain(
            response.Images,
            image =>
                image.BookImageId ==
                originalMainImage.BookImageId);

        Assert.Contains(
            originalMainImage.ImageUrl.TrimStart('/'),
            testContext.FileStorageService.DeletedPaths);

        Assert.Single(
            testContext.FileStorageService.StoredFiles);
    }

    [Fact]
    public async Task DeleteBookImageAsync_WithMissingImage_ThrowsKeyNotFoundException()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        BookDetailsResponse createdBook =
            await testContext.BookService.CreateBookAsync(
                CreateValidRequest(
                    catalog.Publisher.PublisherId,
                    catalog.Author.AuthorId,
                    catalog.Category.CategoryId),
                [
                    testContext.CreateImageFile()
                ],
                mainImageIndex: 0);

        KeyNotFoundException exception =
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () =>
                    testContext.BookService.DeleteBookImageAsync(
                        createdBook.BookId,
                        bookImageId: 999));

        Assert.Equal(
            "BookImageNotFound",
            exception.Message);
    }

    [Fact]
    public async Task DeleteBookAsync_WithExistingBook_PerformsSoftDelete()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        BookDetailsResponse createdBook =
            await testContext.BookService.CreateBookAsync(
                CreateValidRequest(
                    catalog.Publisher.PublisherId,
                    catalog.Author.AuthorId,
                    catalog.Category.CategoryId),
                [
                    testContext.CreateImageFile()
                ],
                mainImageIndex: 0);

        await testContext.BookService.DeleteBookAsync(
            createdBook.BookId);

        testContext.DbContext.ChangeTracker.Clear();

        Book? activeBook =
            await testContext.DbContext.Books
                .SingleOrDefaultAsync(
                    book =>
                        book.BookId ==
                        createdBook.BookId);

        Assert.Null(activeBook);

        Book deletedBook =
            await testContext.DbContext.Books
                .IgnoreQueryFilters()
                .SingleAsync(
                    book =>
                        book.BookId ==
                        createdBook.BookId);

        Assert.True(
            deletedBook.IsDeleted);

        Assert.NotNull(
            deletedBook.DeletedAt);

        Assert.Equal(
            "unit-test-admin-id",
            deletedBook.DeletedById);
    }

    [Fact]
    public async Task DeleteBookAsync_DoesNotPhysicallyDeleteRelationships()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        BookDetailsResponse createdBook =
            await testContext.BookService.CreateBookAsync(
                CreateValidRequest(
                    catalog.Publisher.PublisherId,
                    catalog.Author.AuthorId,
                    catalog.Category.CategoryId),
                [
                    testContext.CreateImageFile()
                ],
                mainImageIndex: 0);

        await testContext.BookService.DeleteBookAsync(
            createdBook.BookId);

        testContext.DbContext.ChangeTracker.Clear();

        Book deletedBook =
            await testContext.DbContext.Books
                .IgnoreQueryFilters()
                .Include(book => book.BookAuthors)
                .Include(book => book.BookCategories)
                .Include(book => book.BookImages)
                .SingleAsync(
                    book =>
                        book.BookId ==
                        createdBook.BookId);

        Assert.True(
            deletedBook.IsDeleted);

        Assert.Single(
            deletedBook.BookAuthors);

        Assert.Single(
            deletedBook.BookCategories);

        Assert.Single(
            deletedBook.BookImages);
    }

    [Fact]
    public async Task DeleteBookAsync_WithMissingBook_ThrowsKeyNotFoundException()
    {
        await using var testContext =
            new BookServiceTestContext();

        KeyNotFoundException exception =
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () =>
                    testContext.BookService.DeleteBookAsync(
                        bookId: 999));

        Assert.Equal(
            "BookNotFound",
            exception.Message);
    }

    [Fact]
    public async Task GetBooksAsync_AfterSoftDelete_ExcludesDeletedBook()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        CreateBookRequest firstRequest =
            CreateValidRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        BookDetailsResponse firstBook =
            await testContext.BookService.CreateBookAsync(
                firstRequest,
                [
                    testContext.CreateImageFile(
                    "first-book.jpg")
                ],
                mainImageIndex: 0);

        CreateBookRequest secondRequest =
            CreateValidRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        secondRequest.Title =
            "Second Active Book";

        secondRequest.Isbn =
            "0-306-40615-2";

        BookDetailsResponse secondBook =
            await testContext.BookService.CreateBookAsync(
                secondRequest,
                [
                    testContext.CreateImageFile(
                    "second-book.jpg")
                ],
                mainImageIndex: 0);

        await testContext.BookService.DeleteBookAsync(
            firstBook.BookId);

        PagedResponse<BookResponse> response =
            await testContext.BookService.GetBooksAsync(
                new BookFilterRequest());

        BookResponse remainingBook =
            Assert.Single(response.Items);

        Assert.Equal(
            secondBook.BookId,
            remainingBook.BookId);

        Assert.Equal(
            "Second Active Book",
            remainingBook.Title);

        Assert.Equal(
            1,
            response.PageNumber);

        Assert.Equal(
            10,
            response.PageSize);

        Assert.Equal(
            1,
            response.TotalCount);

        Assert.Equal(
            1,
            response.TotalPages);

        Assert.False(
            response.HasPreviousPage);

        Assert.False(
            response.HasNextPage);
    }

    private static async Task<(
        Publisher Publisher,
        Author Author,
        Category Category)> SeedCatalogAsync(
            BookServiceTestContext testContext)
    {
        var publisher = new Publisher
        {
            Name = "Test Publisher"
        };

        var author = new Author
        {
            Name = "Test Author"
        };

        var category = new Category
        {
            Name = "Test Category"
        };

        testContext.DbContext.AddRange(
            publisher,
            author,
            category);

        await testContext.DbContext.SaveChangesAsync();

        return (
            publisher,
            author,
            category);
    }

    private static CreateBookRequest CreateValidRequest(
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

    private static UpdateBookRequest
        CreateValidUpdateRequest(
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
                "  Updated description.  ",

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

    [Fact]
    public async Task GetBooksAsync_WithTitleSearch_ReturnsMatchingBook()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        CreateBookRequest firstRequest =
            CreateValidRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        firstRequest.Title = "Introduction to Programming";
        firstRequest.Isbn = "9780131103627";

        await testContext.BookService.CreateBookAsync(
            firstRequest,
            [
                testContext.CreateImageFile(
                "introduction-programming.jpg")
            ],
            mainImageIndex: 0);

        CreateBookRequest secondRequest =
            CreateValidRequest(
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId);

        secondRequest.Title = "Advanced Database Systems";
        secondRequest.Isbn = "9780201309980";

        BookDetailsResponse expectedBook =
            await testContext.BookService.CreateBookAsync(
                secondRequest,
                [
                    testContext.CreateImageFile(
                    "advanced-database.jpg")
                ],
                mainImageIndex: 0);

        BookFilterRequest filter = new()
        {
            SearchTerm = "advanced"
        };

        PagedResponse<BookResponse> response =
            await testContext.BookService.GetBooksAsync(
                filter);

        BookResponse book =
            Assert.Single(response.Items);

        Assert.Equal(
            expectedBook.BookId,
            book.BookId);

        Assert.Equal(
            "Advanced Database Systems",
            book.Title);

        Assert.Equal(
            1,
            response.TotalCount);

        Assert.Equal(
            1,
            response.TotalPages);

        Assert.Equal(
            1,
            response.PageNumber);

        Assert.Equal(
            10,
            response.PageSize);
    }

    [Fact]
    public async Task GetBooksAsync_WithPagination_ReturnsRequestedPage()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        await CreateBookForSearchAsync(
            testContext,
            catalog.Publisher.PublisherId,
            catalog.Author.AuthorId,
            catalog.Category.CategoryId,
            "Alpha Book",
            "9780132350884",
            "alpha-book.jpg");

        await CreateBookForSearchAsync(
            testContext,
            catalog.Publisher.PublisherId,
            catalog.Author.AuthorId,
            catalog.Category.CategoryId,
            "Beta Book",
            "9780201616224",
            "beta-book.jpg");

        await CreateBookForSearchAsync(
            testContext,
            catalog.Publisher.PublisherId,
            catalog.Author.AuthorId,
            catalog.Category.CategoryId,
            "Gamma Book",
            "9780321125217",
            "gamma-book.jpg");

        BookFilterRequest filter = new()
        {
            SortBy = "title",
            SortDirection = "asc",
            PageNumber = 2,
            PageSize = 2
        };

        PagedResponse<BookResponse> response =
            await testContext.BookService.GetBooksAsync(
                filter);

        BookResponse book =
            Assert.Single(response.Items);

        Assert.Equal(
            "Gamma Book",
            book.Title);

        Assert.Equal(
            2,
            response.PageNumber);

        Assert.Equal(
            2,
            response.PageSize);

        Assert.Equal(
            3,
            response.TotalCount);

        Assert.Equal(
            2,
            response.TotalPages);

        Assert.True(
            response.HasPreviousPage);

        Assert.False(
            response.HasNextPage);
    }


    private static void BookAuthorResponseAssertions(
        BookDetailsResponse response,
        Author expectedAuthor)
    {
        BookAuthorResponse responseAuthor =
            Assert.Single(response.Authors);

        Assert.Equal(
            expectedAuthor.AuthorId,
            responseAuthor.AuthorId);

        Assert.Equal(
            expectedAuthor.Name,
            responseAuthor.Name);
    }
    [Fact]
    public async Task GetBooksAsync_WithCatalogFilters_ReturnsMatchingBook()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        BookDetailsResponse expectedBook =
            await CreateBookForSearchAsync(
                testContext,
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId,
                "Catalog Filter Book",
                "9780135957059",
                "catalog-filter-book.jpg");

        BookFilterRequest filter = new()
        {
            PublisherId =
                catalog.Publisher.PublisherId,

            AuthorId =
                catalog.Author.AuthorId,

            CategoryId =
                catalog.Category.CategoryId
        };

        PagedResponse<BookResponse> response =
            await testContext.BookService.GetBooksAsync(
                filter);

        BookResponse book =
            Assert.Single(response.Items);

        Assert.Equal(
            expectedBook.BookId,
            book.BookId);

        Assert.Equal(
            "Catalog Filter Book",
            book.Title);

        Assert.Equal(
            1,
            response.TotalCount);
    }
    [Fact]
    public async Task GetBooksAsync_WithUnknownCategoryId_ReturnsEmptyPage()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        await CreateBookForSearchAsync(
            testContext,
            catalog.Publisher.PublisherId,
            catalog.Author.AuthorId,
            catalog.Category.CategoryId,
            "Existing Category Book",
            "9780134685991",
            "existing-category-book.jpg");

        BookFilterRequest filter = new()
        {
            CategoryId = int.MaxValue
        };

        PagedResponse<BookResponse> response =
            await testContext.BookService.GetBooksAsync(
                filter);

        Assert.Empty(
            response.Items);

        Assert.Equal(
            0,
            response.TotalCount);

        Assert.Equal(
            0,
            response.TotalPages);

        Assert.False(
            response.HasPreviousPage);

        Assert.False(
            response.HasNextPage);
    }
    [Fact]
    public async Task GetBooksAsync_WithLanguageFilter_ReturnsMatchingBook()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        await CreateBookForSearchAsync(
            testContext,
            catalog.Publisher.PublisherId,
            catalog.Author.AuthorId,
            catalog.Category.CategoryId,
            "English Programming Book",
            "9780137903955",
            "english-programming-book.jpg",
            language: "English");

        BookDetailsResponse expectedBook =
            await CreateBookForSearchAsync(
                testContext,
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId,
                "Arabic Programming Book",
                "9780132350884",
                "arabic-programming-book.jpg",
                language: "Arabic");

        BookFilterRequest filter = new()
        {
            Language = "Arabic"
        };

        PagedResponse<BookResponse> response =
            await testContext.BookService.GetBooksAsync(
                filter);

        BookResponse book =
            Assert.Single(response.Items);

        Assert.Equal(
            expectedBook.BookId,
            book.BookId);

        Assert.Equal(
            "Arabic",
            book.Language);

        Assert.Equal(
            1,
            response.TotalCount);
    }
    [Fact]
    public async Task GetBooksAsync_WithPublicationYearFilter_ReturnsMatchingBook()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        await CreateBookForSearchAsync(
            testContext,
            catalog.Publisher.PublisherId,
            catalog.Author.AuthorId,
            catalog.Category.CategoryId,
            "Older Programming Book",
            "9780201633610",
            "older-programming-book.jpg",
            publicationYear: 2018);

        BookDetailsResponse expectedBook =
            await CreateBookForSearchAsync(
                testContext,
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId,
                "Modern Programming Book",
                "9780321146533",
                "modern-programming-book.jpg",
                publicationYear: 2025);

        BookFilterRequest filter = new()
        {
            PublicationYear = 2025
        };

        PagedResponse<BookResponse> response =
            await testContext.BookService.GetBooksAsync(
                filter);

        BookResponse book =
            Assert.Single(response.Items);

        Assert.Equal(
            expectedBook.BookId,
            book.BookId);

        Assert.Equal(
            2025,
            book.PublicationYear);

        Assert.Equal(
            1,
            response.TotalCount);
    }


    [Fact]
    public async Task GetBooksAsync_SortByTitleDescending_ReturnsCorrectOrder()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        await CreateBookForSearchAsync(
            testContext,
            catalog.Publisher.PublisherId,
            catalog.Author.AuthorId,
            catalog.Category.CategoryId,
            "Alpha Book",
            "9780137081073",
            "alpha-sort.jpg");

        await CreateBookForSearchAsync(
            testContext,
            catalog.Publisher.PublisherId,
            catalog.Author.AuthorId,
            catalog.Category.CategoryId,
            "Beta Book",
            "9780134494166",
            "beta-sort.jpg");

        await CreateBookForSearchAsync(
            testContext,
            catalog.Publisher.PublisherId,
            catalog.Author.AuthorId,
            catalog.Category.CategoryId,
            "Gamma Book",
            "9780596007126",
            "gamma-sort.jpg");

        BookFilterRequest filter = new()
        {
            SortBy = "title",
            SortDirection = "desc",
            PageNumber = 1,
            PageSize = 10
        };

        PagedResponse<BookResponse> response =
            await testContext.BookService.GetBooksAsync(
                filter);

        string[] titles =
            response.Items
                .Select(book => book.Title)
                .ToArray();

        Assert.Equal(
            [
                "Gamma Book",
            "Beta Book",
            "Alpha Book"
            ],
            titles);

        Assert.Equal(
            3,
            response.TotalCount);

        Assert.Equal(
            1,
            response.TotalPages);

        Assert.False(
            response.HasPreviousPage);

        Assert.False(
            response.HasNextPage);
    }
    [Fact]
    public async Task GetBooksAsync_WithListingFilters_ReturnsMatchingBook()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        BookDetailsResponse expectedBook =
            await CreateBookForSearchAsync(
                testContext,
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId,
                "Discounted Physical Book",
                "9780131101630",
                "discounted-physical-book.jpg");

        BookDetailsResponse otherBook =
            await CreateBookForSearchAsync(
                testContext,
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId,
                "Digital Book",
                "9780131101647",
                "digital-book.jpg");

        ApplicationUser seller =
            await SeedSellerAsync(
                testContext);

        await AddListingAsync(
            testContext,
            expectedBook.BookId,
            seller,
            price: 100m,
            quantity: 4,
            format: BookFormat.Physical,
            condition: BookCondition.Good,
            discountPercentage: 25m,
            status: ListingStatus.Active);

        await AddListingAsync(
            testContext,
            otherBook.BookId,
            seller,
            price: 40m,
            quantity: 5,
            format: BookFormat.Digital,
            condition: null,
            discountPercentage: 0m,
            status: ListingStatus.Active);

        BookFilterRequest filter = new()
        {
            Format = BookFormat.Physical,
            Condition = BookCondition.Good,
            MinPrice = 70m,
            MaxPrice = 80m,
            InStock = true
        };

        PagedResponse<BookResponse> response =
            await testContext.BookService.GetBooksAsync(
                filter);

        BookResponse book =
            Assert.Single(response.Items);

        Assert.Equal(
            expectedBook.BookId,
            book.BookId);

        Assert.Equal(
            75m,
            book.LowestAvailablePrice);

        Assert.Equal(
            1,
            book.AvailableListingsCount);

        Assert.Equal(
            1,
            response.TotalCount);
    }
    [Fact]
    public async Task GetBooksAsync_WithInStockFalse_ReturnsUnavailableBook()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        BookDetailsResponse availableBook =
            await CreateBookForSearchAsync(
                testContext,
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId,
                "Available Book",
                "9780131101654",
                "available-book.jpg");

        BookDetailsResponse unavailableBook =
            await CreateBookForSearchAsync(
                testContext,
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId,
                "Unavailable Book",
                "9780131101661",
                "unavailable-book.jpg");

        ApplicationUser seller =
            await SeedSellerAsync(
                testContext);

        await AddListingAsync(
            testContext,
            availableBook.BookId,
            seller,
            price: 50m,
            quantity: 3,
            format: BookFormat.Physical,
            condition: BookCondition.New,
            discountPercentage: 0m,
            status: ListingStatus.Active);

        await AddListingAsync(
            testContext,
            unavailableBook.BookId,
            seller,
            price: 30m,
            quantity: 0,
            format: BookFormat.Physical,
            condition: BookCondition.Used,
            discountPercentage: 0m,
            status: ListingStatus.OutOfStock);

        BookFilterRequest filter = new()
        {
            InStock = false
        };

        PagedResponse<BookResponse> response =
            await testContext.BookService.GetBooksAsync(
                filter);

        BookResponse book =
            Assert.Single(response.Items);

        Assert.Equal(
            unavailableBook.BookId,
            book.BookId);

        Assert.Null(
            book.LowestAvailablePrice);

        Assert.Equal(
            0,
            book.AvailableListingsCount);

        Assert.Equal(
            1,
            response.TotalCount);
    }

    [Fact]
    public async Task GetBooksAsync_WithMultipleListings_ReturnsLowestDiscountedPrice()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        BookDetailsResponse createdBook =
            await CreateBookForSearchAsync(
                testContext,
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId,
                "Multiple Listings Book",
                "9780131101678",
                "multiple-listings-book.jpg");

        ApplicationUser seller =
            await SeedSellerAsync(
                testContext);

        await AddListingAsync(
            testContext,
            createdBook.BookId,
            seller,
            price: 100m,
            quantity: 2,
            format: BookFormat.Physical,
            condition: BookCondition.New,
            discountPercentage: 10m,
            status: ListingStatus.Active);

        await AddListingAsync(
            testContext,
            createdBook.BookId,
            seller,
            price: 80m,
            quantity: 5,
            format: BookFormat.Physical,
            condition: BookCondition.Good,
            discountPercentage: 25m,
            status: ListingStatus.Active);

        await AddListingAsync(
            testContext,
            createdBook.BookId,
            seller,
            price: 20m,
            quantity: 0,
            format: BookFormat.Digital,
            condition: null,
            discountPercentage: 0m,
            status: ListingStatus.OutOfStock);

        PagedResponse<BookResponse> response =
            await testContext.BookService.GetBooksAsync(
                new BookFilterRequest());

        BookResponse book =
            Assert.Single(response.Items);

        Assert.Equal(
            60m,
            book.LowestAvailablePrice);

        Assert.Equal(
            2,
            book.AvailableListingsCount);
    }

    [Fact]
    public async Task GetBooksAsync_SortByPublicationYearDescending_ReturnsNewestFirst()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        await CreateBookForSearchAsync(
            testContext,
            catalog.Publisher.PublisherId,
            catalog.Author.AuthorId,
            catalog.Category.CategoryId,
            "Older Book",
            "9780131101685",
            "older-sort-book.jpg",
            publicationYear: 2015);

        await CreateBookForSearchAsync(
            testContext,
            catalog.Publisher.PublisherId,
            catalog.Author.AuthorId,
            catalog.Category.CategoryId,
            "Newest Book",
            "9780131101692",
            "newest-sort-book.jpg",
            publicationYear: 2025);

        await CreateBookForSearchAsync(
            testContext,
            catalog.Publisher.PublisherId,
            catalog.Author.AuthorId,
            catalog.Category.CategoryId,
            "Middle Book",
            "9780131101708",
            "middle-sort-book.jpg",
            publicationYear: 2020);

        BookFilterRequest filter = new()
        {
            SortBy = "publicationYear",
            SortDirection = "desc"
        };

        PagedResponse<BookResponse> response =
            await testContext.BookService.GetBooksAsync(
                filter);

        string[] titles =
            response.Items
                .Select(book => book.Title)
                .ToArray();

        Assert.Equal(
            [
                "Newest Book",
            "Middle Book",
            "Older Book"
            ],
            titles);
    }

    [Fact]
    public async Task GetBooksAsync_SortByPriceAscending_ReturnsCheapestFirst()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        BookDetailsResponse expensiveBook =
            await CreateBookForSearchAsync(
                testContext,
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId,
                "Expensive Book",
                "9780131101715",
                "expensive-book.jpg");

        BookDetailsResponse cheapestBook =
            await CreateBookForSearchAsync(
                testContext,
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId,
                "Cheapest Book",
                "9780131101722",
                "cheapest-book.jpg");

        BookDetailsResponse mediumBook =
            await CreateBookForSearchAsync(
                testContext,
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId,
                "Medium Price Book",
                "9780131101739",
                "medium-price-book.jpg");

        await CreateBookForSearchAsync(
            testContext,
            catalog.Publisher.PublisherId,
            catalog.Author.AuthorId,
            catalog.Category.CategoryId,
            "Book Without Listing",
            "9780131101746",
            "book-without-listing.jpg");

        ApplicationUser seller =
            await SeedSellerAsync(
                testContext);

        await AddListingAsync(
            testContext,
            expensiveBook.BookId,
            seller,
            price: 100m,
            quantity: 2,
            format: BookFormat.Physical,
            condition: BookCondition.New,
            discountPercentage: 0m,
            status: ListingStatus.Active);

        await AddListingAsync(
            testContext,
            cheapestBook.BookId,
            seller,
            price: 50m,
            quantity: 3,
            format: BookFormat.Physical,
            condition: BookCondition.Good,
            discountPercentage: 20m,
            status: ListingStatus.Active);

        await AddListingAsync(
            testContext,
            mediumBook.BookId,
            seller,
            price: 60m,
            quantity: 4,
            format: BookFormat.Digital,
            condition: null,
            discountPercentage: 0m,
            status: ListingStatus.Active);

        BookFilterRequest filter = new()
        {
            SortBy = "price",
            SortDirection = "asc"
        };

        PagedResponse<BookResponse> response =
            await testContext.BookService.GetBooksAsync(
                filter);

        BookResponse[] books =
            response.Items.ToArray();

        Assert.Equal(
            "Cheapest Book",
            books[0].Title);

        Assert.Equal(
            40m,
            books[0].LowestAvailablePrice);

        Assert.Equal(
            "Medium Price Book",
            books[1].Title);

        Assert.Equal(
            60m,
            books[1].LowestAvailablePrice);

        Assert.Equal(
            "Expensive Book",
            books[2].Title);

        Assert.Equal(
            100m,
            books[2].LowestAvailablePrice);

        Assert.Equal(
            "Book Without Listing",
            books[3].Title);

        Assert.Null(
            books[3].LowestAvailablePrice);
    }

    [Fact]
    public async Task GetBooksAsync_SortByAvailableListingsCountDescending_ReturnsHighestFirst()
    {
        await using var testContext =
            new BookServiceTestContext();

        var catalog =
            await SeedCatalogAsync(testContext);

        BookDetailsResponse mostListingsBook =
            await CreateBookForSearchAsync(
                testContext,
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId,
                "Most Listings Book",
                "9780131101753",
                "most-listings-book.jpg");

        BookDetailsResponse oneListingBook =
            await CreateBookForSearchAsync(
                testContext,
                catalog.Publisher.PublisherId,
                catalog.Author.AuthorId,
                catalog.Category.CategoryId,
                "One Listing Book",
                "9780131101760",
                "one-listing-book.jpg");

        await CreateBookForSearchAsync(
            testContext,
            catalog.Publisher.PublisherId,
            catalog.Author.AuthorId,
            catalog.Category.CategoryId,
            "No Listings Book",
            "9780131101777",
            "no-listings-book.jpg");

        ApplicationUser seller =
            await SeedSellerAsync(
                testContext);

        await AddListingAsync(
            testContext,
            mostListingsBook.BookId,
            seller,
            price: 60m,
            quantity: 3,
            format: BookFormat.Physical,
            condition: BookCondition.New,
            discountPercentage: 0m,
            status: ListingStatus.Active);

        await AddListingAsync(
            testContext,
            mostListingsBook.BookId,
            seller,
            price: 50m,
            quantity: 2,
            format: BookFormat.Digital,
            condition: null,
            discountPercentage: 0m,
            status: ListingStatus.Active);

        await AddListingAsync(
            testContext,
            oneListingBook.BookId,
            seller,
            price: 40m,
            quantity: 5,
            format: BookFormat.Physical,
            condition: BookCondition.Good,
            discountPercentage: 0m,
            status: ListingStatus.Active);

        BookFilterRequest filter = new()
        {
            SortBy = "availableListingsCount",
            SortDirection = "desc"
        };

        PagedResponse<BookResponse> response =
            await testContext.BookService.GetBooksAsync(
                filter);

        BookResponse[] books =
            response.Items.ToArray();

        Assert.Equal(
            "Most Listings Book",
            books[0].Title);

        Assert.Equal(
            2,
            books[0].AvailableListingsCount);

        Assert.Equal(
            "One Listing Book",
            books[1].Title);

        Assert.Equal(
            1,
            books[1].AvailableListingsCount);

        Assert.Equal(
            "No Listings Book",
            books[2].Title);

        Assert.Equal(
            0,
            books[2].AvailableListingsCount);
    }



    private static async Task<ApplicationUser>
    SeedSellerAsync(
        BookServiceTestContext testContext)
    {
        ApplicationUser seller = new()
        {
            Id = $"unit-test-seller-{Guid.NewGuid()}",
            UserName =
                $"seller-{Guid.NewGuid()}@example.com",
            NormalizedUserName =
                $"SELLER-{Guid.NewGuid()}@EXAMPLE.COM",
            Email =
                $"seller-{Guid.NewGuid()}@example.com",
            NormalizedEmail =
                $"SELLER-{Guid.NewGuid()}@EXAMPLE.COM",
            FullName = "Unit Test Seller",
            StoreName = "Unit Test Book Store",
            EmailConfirmed = true
        };

        testContext.DbContext.Users.Add(
            seller);

        await testContext.DbContext.SaveChangesAsync();

        return seller;
    }


    private static async Task<Listing>
    AddListingAsync(
        BookServiceTestContext testContext,
        int bookId,
        ApplicationUser seller,
        decimal price,
        int quantity,
        BookFormat format,
        BookCondition? condition,
        decimal discountPercentage,
        ListingStatus status)
    {
        Book book =
            await testContext.DbContext.Books
                .SingleAsync(
                    book =>
                        book.BookId == bookId);

        Listing listing = new()
        {
            BookId = book.BookId,
            Book = book,
            SellerId = seller.Id,
            Seller = seller,
            Price = price,
            Quantity = quantity,
            Format = format,
            Condition = condition,
            DiscountPercentage =
                discountPercentage,
            Status = status
        };

        testContext.DbContext.Listings.Add(
            listing);

        await testContext.DbContext.SaveChangesAsync();

        return listing;
    }

    private static async Task<BookDetailsResponse>
    CreateBookForSearchAsync(
        BookServiceTestContext testContext,
        int publisherId,
        int authorId,
        int categoryId,
        string title,
        string isbn,
        string imageFileName,
        string language = "English",
        int? publicationYear = 2020)
    {
        CreateBookRequest request =
            CreateValidRequest(
                publisherId,
                authorId,
                categoryId);

        request.Title =
            title;

        request.Isbn =
            isbn;

        request.Language =
            language;

        request.PublicationYear =
            publicationYear;

        return await testContext.BookService.CreateBookAsync(
            request,
            [
                testContext.CreateImageFile(
                imageFileName)
            ],
            mainImageIndex: 0);
    }

    private static void BookCategoryResponseAssertions(
        BookDetailsResponse response,
        Category expectedCategory)
    {
        BookCategoryResponse responseCategory =
            Assert.Single(response.Categories);

        Assert.Equal(
            expectedCategory.CategoryId,
            responseCategory.CategoryId);

        Assert.Equal(
            expectedCategory.Name,
            responseCategory.Name);
    }
}