using ElectronicLibrary.BLL.Exceptions;
using ElectronicLibrary.DAL.DTOs.Requests.Authors;
using ElectronicLibrary.DAL.Models.Catalog;
using ElectronicLibrary.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ElectronicLibrary.UnitTests.Services.Catalog;

public class AuthorServiceTests
{
    [Fact]
    public async Task CreateAuthorAsync_WithValidRequest_CreatesAuthor()
    {
        await using var testContext =
            new AuthorServiceTestContext();

        var request = new CreateAuthorRequest
        {
            Name = "  Naguib Mahfouz  ",
            Biography = "  Egyptian novelist.  "
        };

        var response =
            await testContext.AuthorService.CreateAuthorAsync(
                request);

        var savedAuthor =
            await testContext.DbContext.Authors
                .SingleAsync();

        Assert.True(response.AuthorId > 0);
        Assert.Equal("Naguib Mahfouz", response.Name);
        Assert.Equal(
            "Egyptian novelist.",
            response.Biography);
        Assert.Equal(0, response.BooksCount);

        Assert.Equal(
            "Naguib Mahfouz",
            savedAuthor.Name);
        Assert.Equal(
            "Egyptian novelist.",
            savedAuthor.Biography);
        Assert.Equal(
            "unit-test-admin-id",
            savedAuthor.CreatedById);
        Assert.NotEqual(
            default,
            savedAuthor.CreatedAt);
        Assert.False(savedAuthor.IsDeleted);
    }

    [Fact]
    public async Task CreateAuthorAsync_WithDuplicateName_ThrowsConflictException()
    {
        await using var testContext =
            new AuthorServiceTestContext();

        await testContext.AuthorService.CreateAuthorAsync(
            new CreateAuthorRequest
            {
                Name = "Naguib Mahfouz"
            });

        var exception =
            await Assert.ThrowsAsync<ConflictException>(
                () =>
                    testContext.AuthorService
                        .CreateAuthorAsync(
                            new CreateAuthorRequest
                            {
                                Name = "naguib mahfouz"
                            }));

        Assert.Equal(
            "AuthorNameAlreadyExists",
            exception.Message);

        Assert.Equal(
            1,
            await testContext.DbContext.Authors
                .CountAsync());
    }

    [Fact]
    public async Task UpdateAuthorAsync_WithValidRequest_UpdatesAuthor()
    {
        await using var testContext =
            new AuthorServiceTestContext();

        var createdAuthor =
            await testContext.AuthorService.CreateAuthorAsync(
                new CreateAuthorRequest
                {
                    Name = "Old Author Name",
                    Biography = "Old biography"
                });

        var response =
            await testContext.AuthorService.UpdateAuthorAsync(
                createdAuthor.AuthorId,
                new UpdateAuthorRequest
                {
                    Name = "  Updated Author Name  ",
                    Biography = "  Updated biography  "
                });

        var savedAuthor =
            await testContext.DbContext.Authors
                .SingleAsync();

        Assert.Equal(
            "Updated Author Name",
            response.Name);
        Assert.Equal(
            "Updated biography",
            response.Biography);

        Assert.Equal(
            "Updated Author Name",
            savedAuthor.Name);
        Assert.Equal(
            "Updated biography",
            savedAuthor.Biography);
        Assert.Equal(
            "unit-test-admin-id",
            savedAuthor.UpdatedById);
        Assert.NotNull(savedAuthor.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAuthorAsync_WithMissingAuthor_ThrowsKeyNotFoundException()
    {
        await using var testContext =
            new AuthorServiceTestContext();

        var exception =
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () =>
                    testContext.AuthorService.UpdateAuthorAsync(
                        999,
                        new UpdateAuthorRequest
                        {
                            Name = "Missing Author"
                        }));

        Assert.Equal(
            "AuthorNotFound",
            exception.Message);
    }

    [Fact]
    public async Task GetAuthorByIdAsync_WithMissingAuthor_ThrowsKeyNotFoundException()
    {
        await using var testContext =
            new AuthorServiceTestContext();

        var exception =
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () =>
                    testContext.AuthorService
                        .GetAuthorByIdAsync(999));

        Assert.Equal(
            "AuthorNotFound",
            exception.Message);
    }

    [Fact]
    public async Task GetAuthorsAsync_WithPagination_ReturnsCorrectPage()
    {
        await using var testContext =
            new AuthorServiceTestContext();

        testContext.DbContext.Authors.AddRange(
            new Author
            {
                Name = "Charlie Author"
            },
            new Author
            {
                Name = "Alice Author"
            },
            new Author
            {
                Name = "Bob Author"
            });

        await testContext.DbContext.SaveChangesAsync();

        var response =
            await testContext.AuthorService.GetAuthorsAsync(
                new AuthorQueryParameters
                {
                    PageNumber = 1,
                    PageSize = 2
                });

        Assert.Equal(1, response.PageNumber);
        Assert.Equal(2, response.PageSize);
        Assert.Equal(3, response.TotalCount);
        Assert.Equal(2, response.TotalPages);
        Assert.False(response.HasPreviousPage);
        Assert.True(response.HasNextPage);

        Assert.Collection(
            response.Items,
            first =>
                Assert.Equal(
                    "Alice Author",
                    first.Name),
            second =>
                Assert.Equal(
                    "Bob Author",
                    second.Name));
    }

    [Fact]
    public async Task GetAuthorsAsync_WithSearch_ReturnsMatchingAuthors()
    {
        await using var testContext =
            new AuthorServiceTestContext();

        testContext.DbContext.Authors.AddRange(
            new Author
            {
                Name = "Naguib Mahfouz"
            },
            new Author
            {
                Name = "Mahmoud Darwish"
            },
            new Author
            {
                Name = "Ghassan Kanafani"
            });

        await testContext.DbContext.SaveChangesAsync();

        var response =
            await testContext.AuthorService.GetAuthorsAsync(
                new AuthorQueryParameters
                {
                    Search = "Mahfouz",
                    PageNumber = 1,
                    PageSize = 10
                });

        var author = Assert.Single(response.Items);

        Assert.Equal(
            "Naguib Mahfouz",
            author.Name);
        Assert.Equal(1, response.TotalCount);
        Assert.Equal(1, response.TotalPages);
    }

    [Fact]
    public async Task DeleteAuthorAsync_WithoutBooks_SoftDeletesAuthor()
    {
        await using var testContext =
            new AuthorServiceTestContext();

        var createdAuthor =
            await testContext.AuthorService.CreateAuthorAsync(
                new CreateAuthorRequest
                {
                    Name = "Author To Delete"
                });

        await testContext.AuthorService.DeleteAuthorAsync(
            createdAuthor.AuthorId);

        var visibleAuthors =
            await testContext.DbContext.Authors
                .AsNoTracking()
                .ToListAsync();

        var deletedAuthor =
            await testContext.DbContext.Authors
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(
                    author =>
                        author.AuthorId ==
                        createdAuthor.AuthorId);

        Assert.Empty(visibleAuthors);
        Assert.True(deletedAuthor.IsDeleted);
        Assert.NotNull(deletedAuthor.DeletedAt);
        Assert.Equal(
            "unit-test-admin-id",
            deletedAuthor.DeletedById);
    }

    [Fact]
    public async Task DeleteAuthorAsync_WithBooks_ThrowsConflictException()
    {
        await using var testContext =
            new AuthorServiceTestContext();

        var author = new Author
        {
            Name = "Author With Book"
        };

        var publisher = new Publisher
        {
            Name = "Test Publisher"
        };

        var book = new Book
        {
            Title = "Test Book",
            Language = "English",
            Publisher = publisher
        };

        var bookAuthor = new BookAuthor
        {
            Author = author,
            Book = book
        };

        testContext.DbContext.AddRange(
            author,
            publisher,
            book,
            bookAuthor);

        await testContext.DbContext.SaveChangesAsync();

        var exception =
            await Assert.ThrowsAsync<ConflictException>(
                () =>
                    testContext.AuthorService
                        .DeleteAuthorAsync(
                            author.AuthorId));

        Assert.Equal(
            "AuthorHasBooks",
            exception.Message);

        var savedAuthor =
            await testContext.DbContext.Authors
                .SingleAsync();

        Assert.False(savedAuthor.IsDeleted);
        Assert.Null(savedAuthor.DeletedAt);
        Assert.Null(savedAuthor.DeletedById);
    }
}