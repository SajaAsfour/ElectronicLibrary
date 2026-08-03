using ElectronicLibrary.BLL.Exceptions;
using ElectronicLibrary.BLL.Interfaces.Catalog;
using ElectronicLibrary.BLL.Interfaces.Common;
using ElectronicLibrary.DAL.DTOs.Requests.Publishers;
using ElectronicLibrary.DAL.DTOs.Responses.Common;
using ElectronicLibrary.DAL.DTOs.Responses.Publishers;
using ElectronicLibrary.DAL.Models.Catalog;
using ElectronicLibrary.DAL.Repositories.UnitOfWork;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace ElectronicLibrary.BLL.Services.Catalog;

public class PublisherService : IPublisherService
{
    private const int MaximumPageSize = 50;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public PublisherService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResponse<PublisherResponse>>
        GetPublishersAsync(
            PublisherQueryParameters queryParameters,
            CancellationToken cancellationToken = default)
    {
        ValidatePagination(queryParameters);

        var publishersQuery = _unitOfWork
            .Repository<Publisher>()
            .Query()
            .AsNoTracking();

        string? search = queryParameters.Search?.Trim();

        if (!string.IsNullOrWhiteSpace(search))
        {
            publishersQuery = publishersQuery.Where(
                publisher =>
                    publisher.Name.Contains(search));
        }

        int totalCount = await publishersQuery.CountAsync(
            cancellationToken);

        List<PublisherResponse> publishers =
            await publishersQuery
                .OrderBy(publisher => publisher.Name)
                .ThenBy(publisher => publisher.PublisherId)
                .Skip(
                    (queryParameters.PageNumber - 1) *
                    queryParameters.PageSize)
                .Take(queryParameters.PageSize)
                .Select(publisher => new PublisherResponse
                {
                    PublisherId = publisher.PublisherId,
                    Name = publisher.Name,
                    Website = publisher.Website,
                    BooksCount = publisher.Books.Count
                })
                .ToListAsync(cancellationToken);

        return new PagedResponse<PublisherResponse>
        {
            Items = publishers,
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

    public async Task<PublisherDetailsResponse>
        GetPublisherByIdAsync(
            int publisherId,
            CancellationToken cancellationToken = default)
    {
        if (publisherId <= 0)
        {
            throw new KeyNotFoundException(
                "PublisherNotFound");
        }

        Publisher? publisher = await _unitOfWork
            .Repository<Publisher>()
            .Query()
            .AsNoTracking()
            .AsSplitQuery()
            .Include(publisher => publisher.Books)
            .FirstOrDefaultAsync(
                publisher =>
                    publisher.PublisherId == publisherId,
                cancellationToken);

        if (publisher is null)
        {
            throw new KeyNotFoundException(
                "PublisherNotFound");
        }

        return new PublisherDetailsResponse
        {
            PublisherId = publisher.PublisherId,
            Name = publisher.Name,
            Website = publisher.Website,
            CreatedAt = publisher.CreatedAt,
            Books = publisher.Books
                .OrderBy(book => book.Title)
                .ThenBy(book => book.BookId)
                .Select(book => new PublisherBookResponse
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

    public async Task<PublisherResponse>
        CreatePublisherAsync(
            CreatePublisherRequest request,
            CancellationToken cancellationToken = default)
    {
        string normalizedName = request.Name.Trim();

        await EnsureNameIsUniqueAsync(
            normalizedName,
            excludedPublisherId: null,
            cancellationToken);

        Publisher publisher = request.Adapt<Publisher>();

        publisher.Name = normalizedName;
        publisher.Website = NormalizeWebsite(
            request.Website);
        publisher.CreatedAt = DateTime.UtcNow;
        publisher.CreatedById =
            _currentUserService.GetUserId();

        await _unitOfWork
            .Repository<Publisher>()
            .AddAsync(
                publisher,
                cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return await GetPublisherSummaryAsync(
            publisher.PublisherId,
            cancellationToken);
    }

    public async Task<PublisherResponse>
        UpdatePublisherAsync(
            int publisherId,
            UpdatePublisherRequest request,
            CancellationToken cancellationToken = default)
    {
        Publisher publisher =
            await GetPublisherEntityOrThrowAsync(
                publisherId,
                cancellationToken);

        string normalizedName = request.Name.Trim();

        await EnsureNameIsUniqueAsync(
            normalizedName,
            publisherId,
            cancellationToken);

        request.Adapt(publisher);

        publisher.Name = normalizedName;
        publisher.Website = NormalizeWebsite(
            request.Website);
        publisher.UpdatedAt = DateTime.UtcNow;
        publisher.UpdatedById =
            _currentUserService.GetUserId();

        _unitOfWork
            .Repository<Publisher>()
            .Update(publisher);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return await GetPublisherSummaryAsync(
            publisher.PublisherId,
            cancellationToken);
    }

    public async Task DeletePublisherAsync(
        int publisherId,
        CancellationToken cancellationToken = default)
    {
        Publisher publisher =
            await GetPublisherEntityOrThrowAsync(
                publisherId,
                cancellationToken);

        bool hasBooks = await _unitOfWork
            .Repository<Book>()
            .Query()
            .AsNoTracking()
            .AnyAsync(
                book =>
                    book.PublisherId == publisherId,
                cancellationToken);

        if (hasBooks)
        {
            throw new ConflictException(
                "PublisherHasBooks");
        }

        publisher.IsDeleted = true;
        publisher.DeletedAt = DateTime.UtcNow;
        publisher.DeletedById =
            _currentUserService.GetUserId();

        _unitOfWork
            .Repository<Publisher>()
            .Update(publisher);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
    }

    private async Task<Publisher>
        GetPublisherEntityOrThrowAsync(
            int publisherId,
            CancellationToken cancellationToken)
    {
        if (publisherId <= 0)
        {
            throw new KeyNotFoundException(
                "PublisherNotFound");
        }

        Publisher? publisher = await _unitOfWork
            .Repository<Publisher>()
            .GetOneAsync(
                publisher =>
                    publisher.PublisherId == publisherId,
                cancellationToken);

        if (publisher is null)
        {
            throw new KeyNotFoundException(
                "PublisherNotFound");
        }

        return publisher;
    }

    private async Task<PublisherResponse>
        GetPublisherSummaryAsync(
            int publisherId,
            CancellationToken cancellationToken)
    {
        PublisherResponse? publisher = await _unitOfWork
            .Repository<Publisher>()
            .Query()
            .AsNoTracking()
            .Where(publisher =>
                publisher.PublisherId == publisherId)
            .Select(publisher => new PublisherResponse
            {
                PublisherId = publisher.PublisherId,
                Name = publisher.Name,
                Website = publisher.Website,
                BooksCount = publisher.Books.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (publisher is null)
        {
            throw new KeyNotFoundException(
                "PublisherNotFound");
        }

        return publisher;
    }

    private async Task EnsureNameIsUniqueAsync(
        string normalizedName,
        int? excludedPublisherId,
        CancellationToken cancellationToken)
    {
        var query = _unitOfWork
            .Repository<Publisher>()
            .Query()
            .AsNoTracking()
            .Where(publisher =>
                publisher.Name.ToUpper() ==
                normalizedName.ToUpper());

        if (excludedPublisherId.HasValue)
        {
            query = query.Where(
                publisher =>
                    publisher.PublisherId !=
                    excludedPublisherId.Value);
        }

        bool nameExists = await query.AnyAsync(
            cancellationToken);

        if (nameExists)
        {
            throw new ConflictException(
                "PublisherNameAlreadyExists");
        }
    }

    private static string? NormalizeWebsite(
        string? website)
    {
        return string.IsNullOrWhiteSpace(website)
            ? null
            : website.Trim();
    }

    private static void ValidatePagination(
        PublisherQueryParameters queryParameters)
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