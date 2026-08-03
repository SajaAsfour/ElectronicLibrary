using ElectronicLibrary.BLL.Exceptions;
using ElectronicLibrary.BLL.Interfaces.Catalog;
using ElectronicLibrary.BLL.Interfaces.Common;
using ElectronicLibrary.DAL.DTOs.Requests.Categories;
using ElectronicLibrary.DAL.DTOs.Responses.Categories;
using ElectronicLibrary.DAL.DTOs.Responses.Common;
using ElectronicLibrary.DAL.Models.Catalog;
using ElectronicLibrary.DAL.Repositories.UnitOfWork;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace ElectronicLibrary.BLL.Services.Catalog;

public class CategoryService : ICategoryService
{
    private const int MaximumPageSize = 50;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CategoryService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResponse<CategoryResponse>>
        GetCategoriesAsync(
            CategoryQueryParameters queryParameters,
            CancellationToken cancellationToken = default)
    {
        ValidatePagination(queryParameters);

        var categoriesQuery = _unitOfWork
            .Repository<Category>()
            .Query()
            .AsNoTracking();

        string? search = queryParameters.Search?.Trim();

        if (!string.IsNullOrWhiteSpace(search))
        {
            categoriesQuery = categoriesQuery.Where(
                category => category.Name.Contains(search));
        }

        int totalCount = await categoriesQuery.CountAsync(
            cancellationToken);

        List<CategoryResponse> categories =
            await categoriesQuery
                .OrderBy(category => category.Name)
                .ThenBy(category => category.CategoryId)
                .Skip(
                    (queryParameters.PageNumber - 1) *
                    queryParameters.PageSize)
                .Take(queryParameters.PageSize)
                .Select(category => new CategoryResponse
                {
                    CategoryId = category.CategoryId,
                    Name = category.Name,
                    Description = category.Description,
                    BooksCount = category.BookCategories.Count
                })
                .ToListAsync(cancellationToken);

        return new PagedResponse<CategoryResponse>
        {
            Items = categories,
            PageNumber = queryParameters.PageNumber,
            PageSize = queryParameters.PageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    totalCount /
                    (double)queryParameters.PageSize)
        };
    }

    public async Task<CategoryDetailsResponse>
        GetCategoryByIdAsync(
            int categoryId,
            CancellationToken cancellationToken = default)
    {
        if (categoryId <= 0)
        {
            throw new KeyNotFoundException(
                "CategoryNotFound");
        }

        Category? category = await _unitOfWork
            .Repository<Category>()
            .Query()
            .AsNoTracking()
            .AsSplitQuery()
            .Include(category => category.BookCategories)
            .ThenInclude(bookCategory => bookCategory.Book)
            .FirstOrDefaultAsync(
                category =>
                    category.CategoryId == categoryId,
                cancellationToken);

        if (category is null)
        {
            throw new KeyNotFoundException(
                "CategoryNotFound");
        }

        return new CategoryDetailsResponse
        {
            CategoryId = category.CategoryId,
            Name = category.Name,
            Description = category.Description,
            CreatedAt = category.CreatedAt,
            Books = category.BookCategories
                .Select(bookCategory => bookCategory.Book)
                .OrderBy(book => book.Title)
                .ThenBy(book => book.BookId)
                .Select(book => new CategoryBookResponse
                {
                    BookId = book.BookId,
                    Title = book.Title,
                    Isbn = book.Isbn,
                    Language = book.Language,
                    PublicationYear = book.PublicationYear
                })
                .ToList()
        };
    }

    public async Task<CategoryResponse>
        CreateCategoryAsync(
            CreateCategoryRequest request,
            CancellationToken cancellationToken = default)
    {
        string normalizedName = request.Name.Trim();

        await EnsureNameIsUniqueAsync(
            normalizedName,
            excludedCategoryId: null,
            cancellationToken);

        Category category = request.Adapt<Category>();

        category.Name = normalizedName;
        category.Description = NormalizeDescription(
            request.Description);
        category.CreatedAt = DateTime.UtcNow;
        category.CreatedById =
            _currentUserService.GetUserId();

        await _unitOfWork
            .Repository<Category>()
            .AddAsync(
                category,
                cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return await GetCategorySummaryAsync(
            category.CategoryId,
            cancellationToken);
    }

    public async Task<CategoryResponse>
        UpdateCategoryAsync(
            int categoryId,
            UpdateCategoryRequest request,
            CancellationToken cancellationToken = default)
    {
        Category category =
            await GetCategoryEntityOrThrowAsync(
                categoryId,
                cancellationToken);

        string normalizedName = request.Name.Trim();

        await EnsureNameIsUniqueAsync(
            normalizedName,
            categoryId,
            cancellationToken);

        request.Adapt(category);

        category.Name = normalizedName;
        category.Description = NormalizeDescription(
            request.Description);
        category.UpdatedAt = DateTime.UtcNow;
        category.UpdatedById =
            _currentUserService.GetUserId();

        _unitOfWork
            .Repository<Category>()
            .Update(category);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return await GetCategorySummaryAsync(
            category.CategoryId,
            cancellationToken);
    }

    public async Task DeleteCategoryAsync(
        int categoryId,
        CancellationToken cancellationToken = default)
    {
        Category category =
            await GetCategoryEntityOrThrowAsync(
                categoryId,
                cancellationToken);

        bool hasBooks = await _unitOfWork
            .Repository<BookCategory>()
            .Query()
            .AsNoTracking()
            .AnyAsync(
                bookCategory =>
                    bookCategory.CategoryId == categoryId,
                cancellationToken);

        if (hasBooks)
        {
            throw new ConflictException(
                "CategoryHasBooks");
        }

        category.IsDeleted = true;
        category.DeletedAt = DateTime.UtcNow;
        category.DeletedById =
            _currentUserService.GetUserId();

        _unitOfWork
            .Repository<Category>()
            .Update(category);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
    }

    private async Task<Category>
        GetCategoryEntityOrThrowAsync(
            int categoryId,
            CancellationToken cancellationToken)
    {
        if (categoryId <= 0)
        {
            throw new KeyNotFoundException(
                "CategoryNotFound");
        }

        Category? category = await _unitOfWork
            .Repository<Category>()
            .GetOneAsync(
                category =>
                    category.CategoryId == categoryId,
                cancellationToken);

        if (category is null)
        {
            throw new KeyNotFoundException(
                "CategoryNotFound");
        }

        return category;
    }

    private async Task<CategoryResponse>
        GetCategorySummaryAsync(
            int categoryId,
            CancellationToken cancellationToken)
    {
        CategoryResponse? category = await _unitOfWork
            .Repository<Category>()
            .Query()
            .AsNoTracking()
            .Where(category =>
                category.CategoryId == categoryId)
            .Select(category => new CategoryResponse
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                Description = category.Description,
                BooksCount = category.BookCategories.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (category is null)
        {
            throw new KeyNotFoundException(
                "CategoryNotFound");
        }

        return category;
    }

    private async Task EnsureNameIsUniqueAsync(
        string normalizedName,
        int? excludedCategoryId,
        CancellationToken cancellationToken)
    {
        var query = _unitOfWork
            .Repository<Category>()
            .Query()
            .AsNoTracking()
            .Where(category =>
                category.Name.ToUpper() ==
                normalizedName.ToUpper());

        if (excludedCategoryId.HasValue)
        {
            query = query.Where(
                category =>
                    category.CategoryId !=
                    excludedCategoryId.Value);
        }

        bool nameExists = await query.AnyAsync(
            cancellationToken);

        if (nameExists)
        {
            throw new ConflictException(
                "CategoryNameAlreadyExists");
        }
    }

    private static string? NormalizeDescription(
        string? description)
    {
        return string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
    }

    private static void ValidatePagination(
        CategoryQueryParameters queryParameters)
    {
        if (queryParameters.PageNumber < 1 ||
            queryParameters.PageSize < 1 ||
            queryParameters.PageSize > MaximumPageSize)
        {
            throw new InvalidOperationException(
                "InvalidPaginationParameters");
        }
    }
}