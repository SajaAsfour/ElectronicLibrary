using ElectronicLibrary.BLL.Exceptions;
using ElectronicLibrary.DAL.DTOs.Requests.Categories;
using ElectronicLibrary.DAL.Models.Catalog;
using ElectronicLibrary.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ElectronicLibrary.UnitTests.Services.Catalog;

public class CategoryServiceTests
{
    [Fact]
    public async Task CreateCategoryAsync_WithValidRequest_CreatesCategory()
    {
        await using var testContext =
            new CategoryServiceTestContext();

        var request = new CreateCategoryRequest
        {
            Name = "  Science Fiction  ",
            Description = "  Fiction based on science.  "
        };

        var response =
            await testContext.CategoryService
                .CreateCategoryAsync(request);

        var savedCategory =
            await testContext.DbContext.Categories
                .SingleAsync();

        Assert.True(response.CategoryId > 0);
        Assert.Equal("Science Fiction", response.Name);
        Assert.Equal(
            "Fiction based on science.",
            response.Description);
        Assert.Equal(0, response.BooksCount);

        Assert.Equal(
            "Science Fiction",
            savedCategory.Name);
        Assert.Equal(
            "Fiction based on science.",
            savedCategory.Description);
        Assert.Equal(
            "unit-test-admin-id",
            savedCategory.CreatedById);
        Assert.NotEqual(
            default,
            savedCategory.CreatedAt);
        Assert.False(savedCategory.IsDeleted);
    }

    [Fact]
    public async Task CreateCategoryAsync_WithEmptyDescription_SavesDescriptionAsNull()
    {
        await using var testContext =
            new CategoryServiceTestContext();

        var response =
            await testContext.CategoryService
                .CreateCategoryAsync(
                    new CreateCategoryRequest
                    {
                        Name = "History",
                        Description = "   "
                    });

        var savedCategory =
            await testContext.DbContext.Categories
                .SingleAsync();

        Assert.Null(response.Description);
        Assert.Null(savedCategory.Description);
    }

    [Fact]
    public async Task CreateCategoryAsync_WithDuplicateName_ThrowsConflictException()
    {
        await using var testContext =
            new CategoryServiceTestContext();

        await testContext.CategoryService
            .CreateCategoryAsync(
                new CreateCategoryRequest
                {
                    Name = "Science Fiction"
                });

        var exception =
            await Assert.ThrowsAsync<ConflictException>(
                () =>
                    testContext.CategoryService
                        .CreateCategoryAsync(
                            new CreateCategoryRequest
                            {
                                Name = "science fiction"
                            }));

        Assert.Equal(
            "CategoryNameAlreadyExists",
            exception.Message);

        Assert.Equal(
            1,
            await testContext.DbContext.Categories
                .CountAsync());
    }

    [Fact]
    public async Task UpdateCategoryAsync_WithValidRequest_UpdatesCategory()
    {
        await using var testContext =
            new CategoryServiceTestContext();

        var createdCategory =
            await testContext.CategoryService
                .CreateCategoryAsync(
                    new CreateCategoryRequest
                    {
                        Name = "Old Category",
                        Description = "Old description"
                    });

        var response =
            await testContext.CategoryService
                .UpdateCategoryAsync(
                    createdCategory.CategoryId,
                    new UpdateCategoryRequest
                    {
                        Name = "  Updated Category  ",
                        Description =
                            "  Updated description  "
                    });

        var savedCategory =
            await testContext.DbContext.Categories
                .SingleAsync();

        Assert.Equal(
            "Updated Category",
            response.Name);
        Assert.Equal(
            "Updated description",
            response.Description);

        Assert.Equal(
            "Updated Category",
            savedCategory.Name);
        Assert.Equal(
            "Updated description",
            savedCategory.Description);
        Assert.Equal(
            "unit-test-admin-id",
            savedCategory.UpdatedById);
        Assert.NotNull(savedCategory.UpdatedAt);
    }

    [Fact]
    public async Task UpdateCategoryAsync_WithMissingCategory_ThrowsKeyNotFoundException()
    {
        await using var testContext =
            new CategoryServiceTestContext();

        var exception =
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () =>
                    testContext.CategoryService
                        .UpdateCategoryAsync(
                            999,
                            new UpdateCategoryRequest
                            {
                                Name = "Missing Category"
                            }));

        Assert.Equal(
            "CategoryNotFound",
            exception.Message);
    }

    [Fact]
    public async Task GetCategoryByIdAsync_WithMissingCategory_ThrowsKeyNotFoundException()
    {
        await using var testContext =
            new CategoryServiceTestContext();

        var exception =
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () =>
                    testContext.CategoryService
                        .GetCategoryByIdAsync(999));

        Assert.Equal(
            "CategoryNotFound",
            exception.Message);
    }

    [Fact]
    public async Task GetCategoriesAsync_WithPagination_ReturnsCorrectPage()
    {
        await using var testContext =
            new CategoryServiceTestContext();

        testContext.DbContext.Categories.AddRange(
            new Category
            {
                Name = "Thriller"
            },
            new Category
            {
                Name = "Biography"
            },
            new Category
            {
                Name = "Fantasy"
            });

        await testContext.DbContext.SaveChangesAsync();

        var response =
            await testContext.CategoryService
                .GetCategoriesAsync(
                    new CategoryQueryParameters
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
                    "Biography",
                    first.Name),
            second =>
                Assert.Equal(
                    "Fantasy",
                    second.Name));
    }

    [Fact]
    public async Task GetCategoriesAsync_WithSearch_ReturnsMatchingCategories()
    {
        await using var testContext =
            new CategoryServiceTestContext();

        testContext.DbContext.Categories.AddRange(
            new Category
            {
                Name = "Science Fiction"
            },
            new Category
            {
                Name = "Historical Fiction"
            },
            new Category
            {
                Name = "Biography"
            });

        await testContext.DbContext.SaveChangesAsync();

        var response =
            await testContext.CategoryService
                .GetCategoriesAsync(
                    new CategoryQueryParameters
                    {
                        Search = "Fiction",
                        PageNumber = 1,
                        PageSize = 10
                    });

        Assert.Equal(2, response.Items.Count);
        Assert.Equal(2, response.TotalCount);
        Assert.Equal(1, response.TotalPages);

        Assert.Contains(
            response.Items,
            category =>
                category.Name == "Historical Fiction");

        Assert.Contains(
            response.Items,
            category =>
                category.Name == "Science Fiction");
    }

    [Fact]
    public async Task DeleteCategoryAsync_WithoutBooks_SoftDeletesCategory()
    {
        await using var testContext =
            new CategoryServiceTestContext();

        var createdCategory =
            await testContext.CategoryService
                .CreateCategoryAsync(
                    new CreateCategoryRequest
                    {
                        Name = "Category To Delete"
                    });

        await testContext.CategoryService
            .DeleteCategoryAsync(
                createdCategory.CategoryId);

        var visibleCategories =
            await testContext.DbContext.Categories
                .AsNoTracking()
                .ToListAsync();

        var deletedCategory =
            await testContext.DbContext.Categories
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(
                    category =>
                        category.CategoryId ==
                        createdCategory.CategoryId);

        Assert.Empty(visibleCategories);
        Assert.True(deletedCategory.IsDeleted);
        Assert.NotNull(deletedCategory.DeletedAt);
        Assert.Equal(
            "unit-test-admin-id",
            deletedCategory.DeletedById);
    }

    [Fact]
    public async Task DeleteCategoryAsync_WithBooks_ThrowsConflictException()
    {
        await using var testContext =
            new CategoryServiceTestContext();

        var category = new Category
        {
            Name = "Category With Book"
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

        var bookCategory = new BookCategory
        {
            Category = category,
            Book = book
        };

        testContext.DbContext.AddRange(
            category,
            publisher,
            book,
            bookCategory);

        await testContext.DbContext.SaveChangesAsync();

        var exception =
            await Assert.ThrowsAsync<ConflictException>(
                () =>
                    testContext.CategoryService
                        .DeleteCategoryAsync(
                            category.CategoryId));

        Assert.Equal(
            "CategoryHasBooks",
            exception.Message);

        var savedCategory =
            await testContext.DbContext.Categories
                .SingleAsync();

        Assert.False(savedCategory.IsDeleted);
        Assert.Null(savedCategory.DeletedAt);
        Assert.Null(savedCategory.DeletedById);
    }
}