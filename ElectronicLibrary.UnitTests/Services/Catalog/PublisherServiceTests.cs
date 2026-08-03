using ElectronicLibrary.BLL.Exceptions;
using ElectronicLibrary.DAL.DTOs.Requests.Publishers;
using ElectronicLibrary.DAL.Models.Catalog;
using ElectronicLibrary.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ElectronicLibrary.UnitTests.Services.Catalog;

public class PublisherServiceTests
{
    [Fact]
    public async Task CreatePublisherAsync_WithValidRequest_CreatesPublisher()
    {
        await using var testContext =
            new PublisherServiceTestContext();

        var request = new CreatePublisherRequest
        {
            Name = "  Penguin Books  ",
            Website = "  https://www.penguin.com  "
        };

        var response =
            await testContext.PublisherService
                .CreatePublisherAsync(request);

        var savedPublisher =
            await testContext.DbContext.Publishers
                .SingleAsync();

        Assert.True(response.PublisherId > 0);
        Assert.Equal("Penguin Books", response.Name);
        Assert.Equal(
            "https://www.penguin.com",
            response.Website);
        Assert.Equal(0, response.BooksCount);

        Assert.Equal(
            "Penguin Books",
            savedPublisher.Name);
        Assert.Equal(
            "https://www.penguin.com",
            savedPublisher.Website);
        Assert.Equal(
            "unit-test-admin-id",
            savedPublisher.CreatedById);
        Assert.NotEqual(
            default,
            savedPublisher.CreatedAt);
        Assert.False(savedPublisher.IsDeleted);
    }

    [Fact]
    public async Task CreatePublisherAsync_WithEmptyWebsite_SavesWebsiteAsNull()
    {
        await using var testContext =
            new PublisherServiceTestContext();

        var response =
            await testContext.PublisherService
                .CreatePublisherAsync(
                    new CreatePublisherRequest
                    {
                        Name = "Publisher Without Website",
                        Website = "   "
                    });

        var savedPublisher =
            await testContext.DbContext.Publishers
                .SingleAsync();

        Assert.Null(response.Website);
        Assert.Null(savedPublisher.Website);
    }

    [Fact]
    public async Task CreatePublisherAsync_WithDuplicateName_ThrowsConflictException()
    {
        await using var testContext =
            new PublisherServiceTestContext();

        await testContext.PublisherService
            .CreatePublisherAsync(
                new CreatePublisherRequest
                {
                    Name = "Penguin Books"
                });

        var exception =
            await Assert.ThrowsAsync<ConflictException>(
                () =>
                    testContext.PublisherService
                        .CreatePublisherAsync(
                            new CreatePublisherRequest
                            {
                                Name = "penguin books"
                            }));

        Assert.Equal(
            "PublisherNameAlreadyExists",
            exception.Message);

        Assert.Equal(
            1,
            await testContext.DbContext.Publishers
                .CountAsync());
    }

    [Fact]
    public async Task UpdatePublisherAsync_WithValidRequest_UpdatesPublisher()
    {
        await using var testContext =
            new PublisherServiceTestContext();

        var createdPublisher =
            await testContext.PublisherService
                .CreatePublisherAsync(
                    new CreatePublisherRequest
                    {
                        Name = "Old Publisher",
                        Website = "https://old.example.com"
                    });

        var response =
            await testContext.PublisherService
                .UpdatePublisherAsync(
                    createdPublisher.PublisherId,
                    new UpdatePublisherRequest
                    {
                        Name = "  Updated Publisher  ",
                        Website =
                            "  https://updated.example.com  "
                    });

        var savedPublisher =
            await testContext.DbContext.Publishers
                .SingleAsync();

        Assert.Equal(
            "Updated Publisher",
            response.Name);
        Assert.Equal(
            "https://updated.example.com",
            response.Website);

        Assert.Equal(
            "Updated Publisher",
            savedPublisher.Name);
        Assert.Equal(
            "https://updated.example.com",
            savedPublisher.Website);
        Assert.Equal(
            "unit-test-admin-id",
            savedPublisher.UpdatedById);
        Assert.NotNull(savedPublisher.UpdatedAt);
    }

    [Fact]
    public async Task UpdatePublisherAsync_WithMissingPublisher_ThrowsKeyNotFoundException()
    {
        await using var testContext =
            new PublisherServiceTestContext();

        var exception =
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () =>
                    testContext.PublisherService
                        .UpdatePublisherAsync(
                            999,
                            new UpdatePublisherRequest
                            {
                                Name = "Missing Publisher"
                            }));

        Assert.Equal(
            "PublisherNotFound",
            exception.Message);
    }

    [Fact]
    public async Task GetPublisherByIdAsync_WithMissingPublisher_ThrowsKeyNotFoundException()
    {
        await using var testContext =
            new PublisherServiceTestContext();

        var exception =
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () =>
                    testContext.PublisherService
                        .GetPublisherByIdAsync(999));

        Assert.Equal(
            "PublisherNotFound",
            exception.Message);
    }

    [Fact]
    public async Task GetPublishersAsync_WithPagination_ReturnsCorrectPage()
    {
        await using var testContext =
            new PublisherServiceTestContext();

        testContext.DbContext.Publishers.AddRange(
            new Publisher
            {
                Name = "Charlie Publisher"
            },
            new Publisher
            {
                Name = "Alice Publisher"
            },
            new Publisher
            {
                Name = "Bob Publisher"
            });

        await testContext.DbContext.SaveChangesAsync();

        var response =
            await testContext.PublisherService
                .GetPublishersAsync(
                    new PublisherQueryParameters
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
                    "Alice Publisher",
                    first.Name),
            second =>
                Assert.Equal(
                    "Bob Publisher",
                    second.Name));
    }

    [Fact]
    public async Task GetPublishersAsync_WithSearch_ReturnsMatchingPublishers()
    {
        await using var testContext =
            new PublisherServiceTestContext();

        testContext.DbContext.Publishers.AddRange(
            new Publisher
            {
                Name = "Penguin Books"
            },
            new Publisher
            {
                Name = "Oxford University Press"
            },
            new Publisher
            {
                Name = "HarperCollins"
            });

        await testContext.DbContext.SaveChangesAsync();

        var response =
            await testContext.PublisherService
                .GetPublishersAsync(
                    new PublisherQueryParameters
                    {
                        Search = "Oxford",
                        PageNumber = 1,
                        PageSize = 10
                    });

        var publisher =
            Assert.Single(response.Items);

        Assert.Equal(
            "Oxford University Press",
            publisher.Name);
        Assert.Equal(1, response.TotalCount);
        Assert.Equal(1, response.TotalPages);
    }

    [Fact]
    public async Task DeletePublisherAsync_WithoutBooks_SoftDeletesPublisher()
    {
        await using var testContext =
            new PublisherServiceTestContext();

        var createdPublisher =
            await testContext.PublisherService
                .CreatePublisherAsync(
                    new CreatePublisherRequest
                    {
                        Name = "Publisher To Delete"
                    });

        await testContext.PublisherService
            .DeletePublisherAsync(
                createdPublisher.PublisherId);

        var visiblePublishers =
            await testContext.DbContext.Publishers
                .AsNoTracking()
                .ToListAsync();

        var deletedPublisher =
            await testContext.DbContext.Publishers
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(
                    publisher =>
                        publisher.PublisherId ==
                        createdPublisher.PublisherId);

        Assert.Empty(visiblePublishers);
        Assert.True(deletedPublisher.IsDeleted);
        Assert.NotNull(deletedPublisher.DeletedAt);
        Assert.Equal(
            "unit-test-admin-id",
            deletedPublisher.DeletedById);
    }

    [Fact]
    public async Task DeletePublisherAsync_WithBooks_ThrowsConflictException()
    {
        await using var testContext =
            new PublisherServiceTestContext();

        var publisher = new Publisher
        {
            Name = "Publisher With Book"
        };

        var book = new Book
        {
            Title = "Test Book",
            Language = "English",
            Publisher = publisher
        };

        testContext.DbContext.AddRange(
            publisher,
            book);

        await testContext.DbContext.SaveChangesAsync();

        var exception =
            await Assert.ThrowsAsync<ConflictException>(
                () =>
                    testContext.PublisherService
                        .DeletePublisherAsync(
                            publisher.PublisherId));

        Assert.Equal(
            "PublisherHasBooks",
            exception.Message);

        var savedPublisher =
            await testContext.DbContext.Publishers
                .SingleAsync();

        Assert.False(savedPublisher.IsDeleted);
        Assert.Null(savedPublisher.DeletedAt);
        Assert.Null(savedPublisher.DeletedById);
    }
}