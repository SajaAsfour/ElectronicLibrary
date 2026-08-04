using ElectronicLibrary.BLL.Exceptions;
using ElectronicLibrary.BLL.Models.Storage;
using ElectronicLibrary.DAL.DTOs.Requests.Books;
using ElectronicLibrary.DAL.DTOs.Responses.Books;
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

        Assert.True(
            responseImage.ImageUrl.EndsWith(
                ".jpg",
                StringComparison.OrdinalIgnoreCase));

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

        Assert.True(
            mainImage.ImageUrl.EndsWith(
                ".webp",
                StringComparison.OrdinalIgnoreCase));

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

        IReadOnlyCollection<BookResponse> books =
            await testContext.BookService.GetBooksAsync();

        BookResponse remainingBook =
            Assert.Single(books);

        Assert.Equal(
            secondBook.BookId,
            remainingBook.BookId);

        Assert.Equal(
            "Second Active Book",
            remainingBook.Title);
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