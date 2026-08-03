using ElectronicLibrary.BLL.Exceptions;
using ElectronicLibrary.BLL.Interfaces.Catalog;
using ElectronicLibrary.BLL.Interfaces.Common;
using ElectronicLibrary.DAL.DTOs.Requests.Authors;
using ElectronicLibrary.DAL.DTOs.Responses.Authors;
using ElectronicLibrary.DAL.DTOs.Responses.Common;
using ElectronicLibrary.DAL.Models.Catalog;
using ElectronicLibrary.DAL.Repositories.UnitOfWork;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace ElectronicLibrary.BLL.Services.Catalog;

public class AuthorService : IAuthorService
{
    private const int MaximumPageSize = 50;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public AuthorService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResponse<AuthorResponse>> GetAuthorsAsync(
        AuthorQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        ValidatePagination(queryParameters);

        var authorsQuery = _unitOfWork
            .Repository<Author>()
            .Query()
            .AsNoTracking();

        var search = queryParameters.Search?.Trim();

        if (!string.IsNullOrWhiteSpace(search))
        {
            authorsQuery = authorsQuery.Where(
                author => author.Name.Contains(search));
        }

        var totalCount = await authorsQuery.CountAsync(
            cancellationToken);

        var authors = await authorsQuery
            .OrderBy(author => author.Name)
            .ThenBy(author => author.AuthorId)
            .Skip(
                (queryParameters.PageNumber - 1) *
                queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .Select(author => new AuthorResponse
            {
                AuthorId = author.AuthorId,
                Name = author.Name,
                Biography = author.Biography,
                BooksCount = author.BookAuthors.Count
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<AuthorResponse>
        {
            Items = authors,
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

    public async Task<AuthorDetailsResponse> GetAuthorByIdAsync(
        int authorId,
        CancellationToken cancellationToken = default)
    {
        var author = await _unitOfWork
            .Repository<Author>()
            .Query()
            .AsNoTracking()
            .AsSplitQuery()
            .Include(author => author.BookAuthors)
            .ThenInclude(bookAuthor => bookAuthor.Book)
            .FirstOrDefaultAsync(
                author => author.AuthorId == authorId,
                cancellationToken);

        if (author is null)
        {
            throw new KeyNotFoundException(
                "AuthorNotFound");
        }

        return new AuthorDetailsResponse
        {
            AuthorId = author.AuthorId,
            Name = author.Name,
            Biography = author.Biography,
            CreatedAt = author.CreatedAt,
            Books = author.BookAuthors
                .Select(bookAuthor => bookAuthor.Book)
                .OrderBy(book => book.Title)
                .ThenBy(book => book.BookId)
                .Select(book => new AuthorBookResponse
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

    public async Task<AuthorResponse> CreateAuthorAsync(
        CreateAuthorRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = request.Name.Trim();

        await EnsureNameIsUniqueAsync(
            normalizedName,
            excludedAuthorId: null,
            cancellationToken);

        var author = request.Adapt<Author>();

        author.Name = normalizedName;
        author.Biography = NormalizeBiography(
            request.Biography);
        author.CreatedAt = DateTime.UtcNow;
        author.CreatedById =
            _currentUserService.GetUserId();

        await _unitOfWork
            .Repository<Author>()
            .AddAsync(
                author,
                cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return await GetAuthorSummaryAsync(
            author.AuthorId,
            cancellationToken);
    }

    public async Task<AuthorResponse> UpdateAuthorAsync(
        int authorId,
        UpdateAuthorRequest request,
        CancellationToken cancellationToken = default)
    {
        var author = await GetAuthorEntityOrThrowAsync(
            authorId,
            cancellationToken);

        var normalizedName = request.Name.Trim();

        await EnsureNameIsUniqueAsync(
            normalizedName,
            authorId,
            cancellationToken);

        request.Adapt(author);

        author.Name = normalizedName;
        author.Biography = NormalizeBiography(
            request.Biography);
        author.UpdatedAt = DateTime.UtcNow;
        author.UpdatedById =
            _currentUserService.GetUserId();

        _unitOfWork
            .Repository<Author>()
            .Update(author);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return await GetAuthorSummaryAsync(
            author.AuthorId,
            cancellationToken);
    }

    public async Task DeleteAuthorAsync(
        int authorId,
        CancellationToken cancellationToken = default)
    {
        var author = await GetAuthorEntityOrThrowAsync(
            authorId,
            cancellationToken);

        var hasBooks = await _unitOfWork
            .Repository<BookAuthor>()
            .Query()
            .AsNoTracking()
            .AnyAsync(
                bookAuthor =>
                    bookAuthor.AuthorId == authorId,
                cancellationToken);

        if (hasBooks)
        {
            throw new ConflictException(
                "AuthorHasBooks");
        }

        author.IsDeleted = true;
        author.DeletedAt = DateTime.UtcNow;
        author.DeletedById =
            _currentUserService.GetUserId();

        _unitOfWork
            .Repository<Author>()
            .Update(author);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
    }

    private async Task<Author> GetAuthorEntityOrThrowAsync(
        int authorId,
        CancellationToken cancellationToken)
    {
        if (authorId <= 0)
        {
            throw new KeyNotFoundException(
                "AuthorNotFound");
        }

        var author = await _unitOfWork
            .Repository<Author>()
            .GetOneAsync(
                author => author.AuthorId == authorId,
                cancellationToken);

        if (author is null)
        {
            throw new KeyNotFoundException(
                "AuthorNotFound");
        }

        return author;
    }

    private async Task<AuthorResponse> GetAuthorSummaryAsync(
        int authorId,
        CancellationToken cancellationToken)
    {
        var author = await _unitOfWork
            .Repository<Author>()
            .Query()
            .AsNoTracking()
            .Where(author =>
                author.AuthorId == authorId)
            .Select(author => new AuthorResponse
            {
                AuthorId = author.AuthorId,
                Name = author.Name,
                Biography = author.Biography,
                BooksCount = author.BookAuthors.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (author is null)
        {
            throw new KeyNotFoundException(
                "AuthorNotFound");
        }

        return author;
    }

    private async Task EnsureNameIsUniqueAsync(
        string normalizedName,
        int? excludedAuthorId,
        CancellationToken cancellationToken)
    {
        var query = _unitOfWork
            .Repository<Author>()
            .Query()
            .AsNoTracking()
            .Where(author =>
                author.Name.ToUpper() ==
                normalizedName.ToUpper());

        if (excludedAuthorId.HasValue)
        {
            query = query.Where(
                author =>
                    author.AuthorId !=
                    excludedAuthorId.Value);
        }

        var nameExists = await query.AnyAsync(
            cancellationToken);

        if (nameExists)
        {
            throw new ConflictException(
                "AuthorNameAlreadyExists");
        }
    }

    private static string? NormalizeBiography(
        string? biography)
    {
        return string.IsNullOrWhiteSpace(biography)
            ? null
            : biography.Trim();
    }

    private static void ValidatePagination(
        AuthorQueryParameters queryParameters)
    {
        if (queryParameters.PageNumber < 1 ||
            queryParameters.PageSize < 1 ||
            queryParameters.PageSize >
            MaximumPageSize)
        {
            throw new InvalidOperationException(
                "InvalidPaginationParameters");
        }
    }
}