using ElectronicLibrary.BLL.Exceptions;
using ElectronicLibrary.BLL.Helpers.Catalog;
using ElectronicLibrary.BLL.Interfaces.Catalog;
using ElectronicLibrary.BLL.Interfaces.Common;
using ElectronicLibrary.DAL.DTOs.Requests.Books;
using ElectronicLibrary.DAL.DTOs.Responses.Books;
using ElectronicLibrary.DAL.Enums;
using ElectronicLibrary.DAL.Models.Catalog;
using ElectronicLibrary.DAL.Repositories.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using ElectronicLibrary.BLL.Interfaces.Storage;
using ElectronicLibrary.BLL.Models.Storage;
using ElectronicLibrary.DAL.DTOs.Responses.Common;

namespace ElectronicLibrary.BLL.Services.Catalog;

public class BookService : IBookService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorageService _fileStorageService;

    public BookService(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IFileStorageService fileStorageService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _fileStorageService = fileStorageService;
    }

    public async Task<PagedResponse<BookResponse>>
    GetBooksAsync(
        BookFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        int pageNumber =
            request.PageNumber;

        int pageSize =
            request.PageSize;

        IQueryable<Book> query = _unitOfWork
    .Repository<Book>()
    .Query()
    .AsNoTracking();

        string? searchTerm =
    string.IsNullOrWhiteSpace(request.SearchTerm)
        ? null
        : request.SearchTerm
            .Trim()
            .ToLower();

        if (searchTerm is not null)
        {
            query = query.Where(book =>
                book.Title
                    .ToLower()
                    .Contains(searchTerm) ||

                (book.Isbn != null &&
                 book.Isbn
                     .ToLower()
                     .Contains(searchTerm)) ||

                book.Publisher.Name
                    .ToLower()
                    .Contains(searchTerm) ||

                book.BookAuthors.Any(bookAuthor =>
                    bookAuthor.Author.Name
                        .ToLower()
                        .Contains(searchTerm)) ||

                book.BookCategories.Any(bookCategory =>
                    bookCategory.Category.Name
                        .ToLower()
                        .Contains(searchTerm)));
        }

        if (request.PublisherId.HasValue)
        {
            query = query.Where(book =>
                book.PublisherId ==
                request.PublisherId.Value);
        }

        if (request.AuthorId.HasValue)
        {
            query = query.Where(book =>
                book.BookAuthors.Any(bookAuthor =>
                    bookAuthor.AuthorId ==
                    request.AuthorId.Value));
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(book =>
                book.BookCategories.Any(bookCategory =>
                    bookCategory.CategoryId ==
                    request.CategoryId.Value));
        }

        if (!string.IsNullOrWhiteSpace(request.Language))
        {
            string language =
                request.Language.Trim();

            query = query.Where(book =>
                book.Language == language);
        }

        if (request.PublicationYear.HasValue)
        {
            query = query.Where(book =>
                book.PublicationYear ==
                request.PublicationYear.Value);
        }

        if (request.Format.HasValue)
        {
            query = query.Where(book =>
                book.Listings.Any(listing =>
                    listing.Status ==
                        ListingStatus.Active &&
                    listing.Format ==
                        request.Format.Value));
        }

        if (request.Condition.HasValue)
        {
            query = query.Where(book =>
                book.Listings.Any(listing =>
                    listing.Status ==
                        ListingStatus.Active &&
                    listing.Condition ==
                        request.Condition.Value));
        }

        if (request.MinPrice.HasValue)
        {
            decimal minPrice =
                request.MinPrice.Value;

            query = query.Where(book =>
                book.Listings.Any(listing =>
                    listing.Status ==
                        ListingStatus.Active &&
                    listing.Quantity > 0 &&
                    listing.Price -
                        listing.Price *
                        listing.DiscountPercentage /
                        100m >= minPrice));
        }

        if (request.MaxPrice.HasValue)
        {
            decimal maxPrice =
                request.MaxPrice.Value;

            query = query.Where(book =>
                book.Listings.Any(listing =>
                    listing.Status ==
                        ListingStatus.Active &&
                    listing.Quantity > 0 &&
                    listing.Price -
                        listing.Price *
                        listing.DiscountPercentage /
                        100m <= maxPrice));
        }

        if (request.InStock.HasValue)
        {
            if (request.InStock.Value)
            {
                query = query.Where(book =>
                    book.Listings.Any(listing =>
                        listing.Status ==
                            ListingStatus.Active &&
                        listing.Quantity > 0));
            }
            else
            {
                query = query.Where(book =>
                    !book.Listings.Any(listing =>
                        listing.Status ==
                            ListingStatus.Active &&
                        listing.Quantity > 0));
            }
        }

        int totalCount =
            await query.CountAsync(
                cancellationToken);

        string sortBy =
    string.IsNullOrWhiteSpace(request.SortBy)
        ? "title"
        : request.SortBy
            .Trim()
            .ToLowerInvariant();

        bool sortDescending =
            string.Equals(
                request.SortDirection?.Trim(),
                "desc",
                StringComparison.OrdinalIgnoreCase);

        IOrderedQueryable<Book> orderedQuery;

        switch (sortBy)
        {
            case "publicationyear":
            case "year":
                orderedQuery = sortDescending
                    ? query
                        .OrderBy(book =>
                            book.PublicationYear == null)
                        .ThenByDescending(book =>
                            book.PublicationYear)
                        .ThenBy(book =>
                            book.BookId)
                    : query
                        .OrderBy(book =>
                            book.PublicationYear == null)
                        .ThenBy(book =>
                            book.PublicationYear)
                        .ThenBy(book =>
                            book.BookId);

                break;

            case "price":
            case "lowestprice":
                orderedQuery = sortDescending
                    ? query
                        .OrderBy(book =>
                            !book.Listings.Any(listing =>
                                listing.Status ==
                                    ListingStatus.Active &&
                                listing.Quantity > 0))
                        .ThenByDescending(book =>
                            book.Listings
                                .Where(listing =>
                                    listing.Status ==
                                        ListingStatus.Active &&
                                    listing.Quantity > 0)
                                .Select(listing =>
                                    (decimal?)(
                                        listing.Price -
                                        listing.Price *
                                        listing.DiscountPercentage /
                                        100m))
                                .Min())
                        .ThenBy(book =>
                            book.BookId)
                    : query
                        .OrderBy(book =>
                            !book.Listings.Any(listing =>
                                listing.Status ==
                                    ListingStatus.Active &&
                                listing.Quantity > 0))
                        .ThenBy(book =>
                            book.Listings
                                .Where(listing =>
                                    listing.Status ==
                                        ListingStatus.Active &&
                                    listing.Quantity > 0)
                                .Select(listing =>
                                    (decimal?)(
                                        listing.Price -
                                        listing.Price *
                                        listing.DiscountPercentage /
                                        100m))
                                .Min())
                        .ThenBy(book =>
                            book.BookId);

                break;

            case "availablelistingscount":
            case "listingscount":
                orderedQuery = sortDescending
                    ? query
                        .OrderByDescending(book =>
                            book.Listings.Count(listing =>
                                listing.Status ==
                                    ListingStatus.Active &&
                                listing.Quantity > 0))
                        .ThenBy(book =>
                            book.BookId)
                    : query
                        .OrderBy(book =>
                            book.Listings.Count(listing =>
                                listing.Status ==
                                    ListingStatus.Active &&
                                listing.Quantity > 0))
                        .ThenBy(book =>
                            book.BookId);

                break;

            case "title":
            default:
                orderedQuery = sortDescending
                    ? query
                        .OrderByDescending(book =>
                            book.Title)
                        .ThenBy(book =>
                            book.BookId)
                    : query
                        .OrderBy(book =>
                            book.Title)
                        .ThenBy(book =>
                            book.BookId);

                break;
        }

        List<BookResponse> books = await orderedQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(book => new BookResponse
        {
                BookId = book.BookId,
                Title = book.Title,
                Isbn = book.Isbn,
                Language = book.Language,
                PublicationYear = book.PublicationYear,
                PublisherName = book.Publisher.Name,

                Authors = book.BookAuthors
                    .OrderBy(bookAuthor =>
                        bookAuthor.Author.Name)
                    .Select(bookAuthor =>
                        bookAuthor.Author.Name)
                    .ToList(),

                Categories = book.BookCategories
                    .OrderBy(bookCategory =>
                        bookCategory.Category.Name)
                    .Select(bookCategory =>
                        bookCategory.Category.Name)
                    .ToList(),

                MainImageUrl = book.BookImages
                    .Where(image => image.IsMain)
                    .Select(image => image.ImageUrl)
                    .FirstOrDefault(),

                LowestAvailablePrice = book.Listings
                    .Where(listing =>
                        listing.Status ==
                            ListingStatus.Active &&
                        listing.Quantity > 0)
                    .Select(listing =>
                        (decimal?)(
                            listing.Price -
                            listing.Price *
                            listing.DiscountPercentage /
                            100m))
                    .Min(),

                AvailableListingsCount =
                    book.Listings.Count(
                        listing =>
                            listing.Status ==
                                ListingStatus.Active &&
                            listing.Quantity > 0)
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<BookResponse>
        {
            Items = books,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    totalCount /
                    (double)pageSize)
        };
    }

    public async Task<BookDetailsResponse>
        GetBookByIdAsync(
            int bookId,
            CancellationToken cancellationToken = default)
    {
        return await GetBookDetailsAsync(
            bookId,
            cancellationToken);
    }

    public async Task<BookDetailsResponse>
    CreateBookAsync(
        CreateBookRequest request,
        IReadOnlyCollection<FileUploadData> images,
        int? mainImageIndex,
        CancellationToken cancellationToken = default)
    {
        List<FileUploadData> uploadFiles =
            ValidateUploadFiles(
                images,
                mainImageIndex,
                imagesRequired: true);

        string title =
            BookValidationHelper.NormalizeRequiredText(
                request.Title,
                "BookTitleRequired");

        string language =
            BookValidationHelper.NormalizeRequiredText(
                request.Language,
                "BookLanguageRequired");

        string? description =
            BookValidationHelper.NormalizeOptionalText(
                request.Description);

        string? normalizedIsbn =
            BookValidationHelper.NormalizeIsbn(
                request.Isbn);

        BookValidationHelper.ValidatePublicationYear(
            request.PublicationYear);

        int[] authorIds =
            BookValidationHelper.ValidateAuthorIds(
                    request.AuthorIds)
                .ToArray();

        int[] categoryIds =
            BookValidationHelper.ValidateCategoryIds(
                    request.CategoryIds)
                .ToArray();

        await ValidateRelationshipsAsync(
            request.PublisherId,
            authorIds,
            categoryIds,
            cancellationToken);

        await EnsureIsbnIsUniqueAsync(
            normalizedIsbn,
            excludedBookId: null,
            cancellationToken);

        var book = new Book
        {
            Title = title,
            Isbn = normalizedIsbn,
            Description = description,
            Language = language,
            PublicationYear = request.PublicationYear,
            PublisherId = request.PublisherId,
            CreatedAt = DateTime.UtcNow,
            CreatedById =
                _currentUserService.GetUserId()
        };

        foreach (int authorId in authorIds)
        {
            book.BookAuthors.Add(
                new BookAuthor
                {
                    AuthorId = authorId
                });
        }

        foreach (int categoryId in categoryIds)
        {
            book.BookCategories.Add(
                new BookCategory
                {
                    CategoryId = categoryId
                });
        }

        await _unitOfWork
            .Repository<Book>()
            .AddAsync(
                book,
                cancellationToken);

        /*
         * أول حفظ للحصول على BookId؛ لأن الصور
         * ستُحفظ في uploads/books/{bookId}.
         */
        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        List<StoredFileResult> storedFiles = [];

        try
        {
            storedFiles =
                await StoreBookImageFilesAsync(
                    book.BookId,
                    uploadFiles,
                    cancellationToken);

            AddStoredImagesToBook(
                book,
                storedFiles,
                mainImageIndex);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }
        catch
        {
            await DeleteStoredFilesQuietlyAsync(
                storedFiles);

            await DeleteCreatedBookQuietlyAsync(
                book);

            throw;
        }

        return await GetBookDetailsAsync(
            book.BookId,
            cancellationToken);
    }

    public async Task<BookDetailsResponse>
    UpdateBookAsync(
        int bookId,
        UpdateBookRequest request,
        IReadOnlyCollection<FileUploadData> images,
        int? mainImageIndex,
        CancellationToken cancellationToken = default)
    {
        List<FileUploadData> uploadFiles =
            ValidateUploadFiles(
                images,
                mainImageIndex,
                imagesRequired: false);

        Book book =
            await GetTrackedBookOrThrowAsync(
                bookId,
                cancellationToken);

        if (book.BookImages.Count +
            uploadFiles.Count > 10)
        {
            throw new InvalidOperationException(
                "MaximumBookImagesExceeded");
        }

        string title =
            BookValidationHelper.NormalizeRequiredText(
                request.Title,
                "BookTitleRequired");

        string language =
            BookValidationHelper.NormalizeRequiredText(
                request.Language,
                "BookLanguageRequired");

        string? description =
            BookValidationHelper.NormalizeOptionalText(
                request.Description);

        string? normalizedIsbn =
            BookValidationHelper.NormalizeIsbn(
                request.Isbn);

        BookValidationHelper.ValidatePublicationYear(
            request.PublicationYear);

        int[] authorIds =
            BookValidationHelper.ValidateAuthorIds(
                    request.AuthorIds)
                .ToArray();

        int[] categoryIds =
            BookValidationHelper.ValidateCategoryIds(
                    request.CategoryIds)
                .ToArray();

        await ValidateRelationshipsAsync(
            request.PublisherId,
            authorIds,
            categoryIds,
            cancellationToken);

        await EnsureIsbnIsUniqueAsync(
            normalizedIsbn,
            bookId,
            cancellationToken);

        book.Title = title;
        book.Isbn = normalizedIsbn;
        book.Description = description;
        book.Language = language;
        book.PublicationYear =
            request.PublicationYear;
        book.PublisherId =
            request.PublisherId;

        UpdateAuthorRelationships(
            book,
            authorIds);

        UpdateCategoryRelationships(
            book,
            categoryIds);

        List<StoredFileResult> storedFiles = [];

        try
        {
            if (uploadFiles.Count > 0)
            {
                storedFiles =
                    await StoreBookImageFilesAsync(
                        book.BookId,
                        uploadFiles,
                        cancellationToken);

                AddStoredImagesToBook(
                    book,
                    storedFiles,
                    mainImageIndex);
            }

            MarkBookAsUpdated(book);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }
        catch
        {
            await DeleteStoredFilesQuietlyAsync(
                storedFiles);

            throw;
        }

        return await GetBookDetailsAsync(
            book.BookId,
            cancellationToken);
    }
    private static List<FileUploadData>
    ValidateUploadFiles(
        IReadOnlyCollection<FileUploadData>? images,
        int? mainImageIndex,
        bool imagesRequired)
    {
        if (images is null || images.Count == 0)
        {
            if (imagesRequired)
            {
                throw new InvalidOperationException(
                    "BookImagesRequired");
            }

            if (mainImageIndex.HasValue)
            {
                throw new InvalidOperationException(
                    "InvalidMainImageIndex");
            }

            return [];
        }

        List<FileUploadData> uploadFiles =
            images.ToList();

        if (uploadFiles.Count > 10)
        {
            throw new InvalidOperationException(
                "MaximumBookImagesExceeded");
        }

        if (mainImageIndex.HasValue &&
            (mainImageIndex.Value < 0 ||
             mainImageIndex.Value >= uploadFiles.Count))
        {
            throw new InvalidOperationException(
                "InvalidMainImageIndex");
        }

        return uploadFiles;
    }
    private async Task<List<StoredFileResult>>
    StoreBookImageFilesAsync(
        int bookId,
        IReadOnlyList<FileUploadData> files,
        CancellationToken cancellationToken)
    {
        List<StoredFileResult> storedFiles = [];

        try
        {
            foreach (FileUploadData file in files)
            {
                StoredFileResult storedFile =
                    await _fileStorageService
                        .SaveBookImageAsync(
                            bookId,
                            file.Content,
                            file.FileName,
                            file.ContentType,
                            file.Length,
                            cancellationToken);

                storedFiles.Add(storedFile);
            }

            return storedFiles;
        }
        catch
        {
            await DeleteStoredFilesQuietlyAsync(
                storedFiles);

            throw;
        }
    }
    private static void AddStoredImagesToBook(
    Book book,
    IReadOnlyList<StoredFileResult> storedFiles,
    int? mainImageIndex)
    {
        bool alreadyHasMainImage =
            book.BookImages.Any(
                image => image.IsMain);

        if (mainImageIndex.HasValue)
        {
            foreach (BookImage existingImage
                     in book.BookImages)
            {
                existingImage.IsMain = false;
            }
        }

        bool makeFirstUploadedImageMain =
            !alreadyHasMainImage &&
            !mainImageIndex.HasValue;

        for (int index = 0;
             index < storedFiles.Count;
             index++)
        {
            StoredFileResult storedFile =
                storedFiles[index];

            bool isMain =
                mainImageIndex == index ||
                (makeFirstUploadedImageMain &&
                 index == 0);

            book.BookImages.Add(
                new BookImage
                {
                    BookId = book.BookId,
                    ImageUrl = storedFile.PublicUrl,
                    IsMain = isMain
                });
        }
    }
    private async Task DeleteStoredFilesQuietlyAsync(
    IEnumerable<StoredFileResult> storedFiles)
    {
        foreach (StoredFileResult storedFile
                 in storedFiles)
        {
            try
            {
                await _fileStorageService.DeleteFileAsync(
                    storedFile.RelativePath,
                    CancellationToken.None);
            }
            catch
            {
                // Keep the original exception.
            }
        }
    }
    private async Task DeleteCreatedBookQuietlyAsync(
    Book book)
    {
        try
        {
            book.BookImages.Clear();

            _unitOfWork
                .Repository<Book>()
                .Delete(book);

            await _unitOfWork.SaveChangesAsync(
                CancellationToken.None);
        }
        catch
        {
            // Keep the original upload/database exception.
        }
    }


    public async Task DeleteBookAsync(
        int bookId,
        CancellationToken cancellationToken = default)
    {
        Book? book = await _unitOfWork
            .Repository<Book>()
            .GetOneAsync(
                book => book.BookId == bookId,
                cancellationToken);

        if (book is null)
        {
            throw new KeyNotFoundException(
                "BookNotFound");
        }

        book.IsDeleted = true;
        book.DeletedAt = DateTime.UtcNow;
        book.DeletedById =
            _currentUserService.GetUserId();

        _unitOfWork
            .Repository<Book>()
            .Update(book);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
    }

    private async Task<BookDetailsResponse>
        GetBookDetailsAsync(
            int bookId,
            CancellationToken cancellationToken)
    {
        if (bookId <= 0)
        {
            throw new KeyNotFoundException(
                "BookNotFound");
        }

        BookDetailsResponse? book = await _unitOfWork
            .Repository<Book>()
            .Query()
            .AsNoTracking()
            .AsSplitQuery()
            .Where(book => book.BookId == bookId)
            .Select(book => new BookDetailsResponse
            {
                BookId = book.BookId,
                Title = book.Title,
                Isbn = book.Isbn,
                Description = book.Description,
                Language = book.Language,
                PublicationYear = book.PublicationYear,
                CreatedAt = book.CreatedAt,

                Publisher = new BookPublisherResponse
                {
                    PublisherId =
                        book.Publisher.PublisherId,
                    Name = book.Publisher.Name,
                    Website = book.Publisher.Website
                },

                Authors = book.BookAuthors
                    .OrderBy(bookAuthor =>
                        bookAuthor.Author.Name)
                    .ThenBy(bookAuthor =>
                        bookAuthor.AuthorId)
                    .Select(bookAuthor =>
                        new BookAuthorResponse
                        {
                            AuthorId =
                                bookAuthor.AuthorId,
                            Name =
                                bookAuthor.Author.Name
                        })
                    .ToList(),

                Categories = book.BookCategories
                    .OrderBy(bookCategory =>
                        bookCategory.Category.Name)
                    .ThenBy(bookCategory =>
                        bookCategory.CategoryId)
                    .Select(bookCategory =>
                        new BookCategoryResponse
                        {
                            CategoryId =
                                bookCategory.CategoryId,
                            Name =
                                bookCategory.Category.Name
                        })
                    .ToList(),

                Images = book.BookImages
                    .OrderByDescending(image =>
                        image.IsMain)
                    .ThenBy(image =>
                        image.BookImageId)
                    .Select(image =>
                        new BookImageResponse
                        {
                            BookImageId =
                                image.BookImageId,
                            ImageUrl =
                                image.ImageUrl,
                            IsMain =
                                image.IsMain
                        })
                    .ToList(),

                AvailableListingsCount =
                    book.Listings.Count(
                        listing =>
                            listing.Status ==
                                ListingStatus.Active &&
                            listing.Quantity > 0)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (book is null)
        {
            throw new KeyNotFoundException(
                "BookNotFound");
        }

        return book;
    }

    private async Task<Book>
        GetTrackedBookOrThrowAsync(
            int bookId,
            CancellationToken cancellationToken)
    {
        if (bookId <= 0)
        {
            throw new KeyNotFoundException(
                "BookNotFound");
        }

        Book? book = await _unitOfWork
            .Repository<Book>()
            .Query()
            .AsSplitQuery()
            .Include(book => book.BookAuthors)
            .Include(book => book.BookCategories)
            .Include(book => book.BookImages)
            .FirstOrDefaultAsync(
                book => book.BookId == bookId,
                cancellationToken);

        if (book is null)
        {
            throw new KeyNotFoundException(
                "BookNotFound");
        }

        return book;
    }

    private async Task ValidateRelationshipsAsync(
        int publisherId,
        IReadOnlyCollection<int> authorIds,
        IReadOnlyCollection<int> categoryIds,
        CancellationToken cancellationToken)
    {
        bool publisherExists = await _unitOfWork
            .Repository<Publisher>()
            .Query()
            .AsNoTracking()
            .AnyAsync(
                publisher =>
                    publisher.PublisherId ==
                    publisherId,
                cancellationToken);

        if (!publisherExists)
        {
            throw new KeyNotFoundException(
                "PublisherNotFound");
        }

        int existingAuthorsCount = await _unitOfWork
            .Repository<Author>()
            .Query()
            .AsNoTracking()
            .CountAsync(
                author =>
                    authorIds.Contains(
                        author.AuthorId),
                cancellationToken);

        if (existingAuthorsCount != authorIds.Count)
        {
            throw new KeyNotFoundException(
                "OneOrMoreAuthorsNotFound");
        }

        int existingCategoriesCount = await _unitOfWork
            .Repository<Category>()
            .Query()
            .AsNoTracking()
            .CountAsync(
                category =>
                    categoryIds.Contains(
                        category.CategoryId),
                cancellationToken);

        if (existingCategoriesCount != categoryIds.Count)
        {
            throw new KeyNotFoundException(
                "OneOrMoreCategoriesNotFound");
        }
    }

    private async Task EnsureIsbnIsUniqueAsync(
        string? normalizedIsbn,
        int? excludedBookId,
        CancellationToken cancellationToken)
    {
        if (normalizedIsbn is null)
        {
            return;
        }

        var query = _unitOfWork
            .Repository<Book>()
            .Query()
            .AsNoTracking()
            .Where(book =>
                book.Isbn == normalizedIsbn);

        if (excludedBookId.HasValue)
        {
            query = query.Where(
                book =>
                    book.BookId !=
                    excludedBookId.Value);
        }

        bool isbnExists = await query.AnyAsync(
            cancellationToken);

        if (isbnExists)
        {
            throw new ConflictException(
                "BookIsbnAlreadyExists");
        }
    }

    private void UpdateAuthorRelationships(
        Book book,
        IReadOnlyCollection<int> authorIds)
    {
        HashSet<int> requestedAuthorIds =
            authorIds.ToHashSet();

        List<BookAuthor> relationshipsToRemove =
            book.BookAuthors
                .Where(bookAuthor =>
                    !requestedAuthorIds.Contains(
                        bookAuthor.AuthorId))
                .ToList();

        foreach (BookAuthor relationship
                 in relationshipsToRemove)
        {
            book.BookAuthors.Remove(relationship);

            _unitOfWork
                .Repository<BookAuthor>()
                .Delete(relationship);
        }

        HashSet<int> existingAuthorIds =
            book.BookAuthors
                .Select(bookAuthor =>
                    bookAuthor.AuthorId)
                .ToHashSet();

        foreach (int authorId in requestedAuthorIds)
        {
            if (existingAuthorIds.Contains(authorId))
            {
                continue;
            }

            book.BookAuthors.Add(
                new BookAuthor
                {
                    BookId = book.BookId,
                    AuthorId = authorId
                });
        }
    }

    private void UpdateCategoryRelationships(
        Book book,
        IReadOnlyCollection<int> categoryIds)
    {
        HashSet<int> requestedCategoryIds =
            categoryIds.ToHashSet();

        List<BookCategory> relationshipsToRemove =
            book.BookCategories
                .Where(bookCategory =>
                    !requestedCategoryIds.Contains(
                        bookCategory.CategoryId))
                .ToList();

        foreach (BookCategory relationship
                 in relationshipsToRemove)
        {
            book.BookCategories.Remove(relationship);

            _unitOfWork
                .Repository<BookCategory>()
                .Delete(relationship);
        }

        HashSet<int> existingCategoryIds =
            book.BookCategories
                .Select(bookCategory =>
                    bookCategory.CategoryId)
                .ToHashSet();

        foreach (int categoryId in requestedCategoryIds)
        {
            if (existingCategoryIds.Contains(categoryId))
            {
                continue;
            }

            book.BookCategories.Add(
                new BookCategory
                {
                    BookId = book.BookId,
                    CategoryId = categoryId
                });
        }
    }
    public async Task<UploadBookImagesResponse>
    UploadBookImagesAsync(
        int bookId,
        IReadOnlyCollection<FileUploadData> files,
        int? mainImageIndex,
        CancellationToken cancellationToken = default)
    {
        if (files is null || files.Count == 0)
        {
            throw new InvalidOperationException(
                "BookImagesRequired");
        }

        List<FileUploadData> uploadFiles =
            files.ToList();

        if (mainImageIndex.HasValue &&
            (mainImageIndex.Value < 0 ||
             mainImageIndex.Value >= uploadFiles.Count))
        {
            throw new InvalidOperationException(
                "InvalidMainImageIndex");
        }

        Book book =
            await GetTrackedBookWithImagesOrThrowAsync(
                bookId,
                cancellationToken);

        if (book.BookImages.Count + uploadFiles.Count > 10)
        {
            throw new InvalidOperationException(
                "MaximumBookImagesExceeded");
        }

        bool bookAlreadyHasMainImage =
            book.BookImages.Any(image => image.IsMain);

        bool makeFirstUploadedImageMain =
            !bookAlreadyHasMainImage &&
            !mainImageIndex.HasValue;

        if (mainImageIndex.HasValue)
        {
            foreach (BookImage existingImage
                     in book.BookImages)
            {
                existingImage.IsMain = false;
            }
        }

        List<StoredFileResult> storedFiles = [];

        try
        {
            for (int index = 0;
                 index < uploadFiles.Count;
                 index++)
            {
                FileUploadData file =
                    uploadFiles[index];

                StoredFileResult storedFile =
                    await _fileStorageService
                        .SaveBookImageAsync(
                            book.BookId,
                            file.Content,
                            file.FileName,
                            file.ContentType,
                            file.Length,
                            cancellationToken);

                storedFiles.Add(storedFile);

                bool isMain =
                    mainImageIndex == index ||
                    (makeFirstUploadedImageMain &&
                     index == 0);

                book.BookImages.Add(
                    new BookImage
                    {
                        BookId = book.BookId,
                        ImageUrl = storedFile.PublicUrl,
                        IsMain = isMain
                    });
            }

            MarkBookAsUpdated(book);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }
        catch
        {
            foreach (StoredFileResult storedFile
                     in storedFiles)
            {
                try
                {
                    await _fileStorageService.DeleteFileAsync(
                        storedFile.RelativePath,
                        CancellationToken.None);
                }
                catch
                {
                    // Preserve the original upload/database exception.
                }
            }

            throw;
        }

        return BuildBookImagesResponse(book);
    }
    public async Task<UploadBookImagesResponse>
    DeleteBookImageAsync(
        int bookId,
        int bookImageId,
        CancellationToken cancellationToken = default)
    {
        Book book =
            await GetTrackedBookWithImagesOrThrowAsync(
                bookId,
                cancellationToken);

        BookImage? image = book.BookImages
            .FirstOrDefault(
                bookImage =>
                    bookImage.BookImageId ==
                    bookImageId);

        if (image is null)
        {
            throw new KeyNotFoundException(
                "BookImageNotFound");
        }

        bool deletedImageWasMain =
            image.IsMain;

        string storedImagePath =
            image.ImageUrl;

        book.BookImages.Remove(image);

        _unitOfWork
            .Repository<BookImage>()
            .Delete(image);

        if (deletedImageWasMain)
        {
            BookImage? replacementMainImage =
                book.BookImages
                    .OrderBy(bookImage =>
                        bookImage.BookImageId)
                    .FirstOrDefault();

            if (replacementMainImage is not null)
            {
                replacementMainImage.IsMain = true;
            }
        }

        MarkBookAsUpdated(book);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        try
        {
            await _fileStorageService.DeleteFileAsync(
                storedImagePath,
                CancellationToken.None);
        }
        catch
        {
            // The database remains correct even if an orphan file
            // could not be physically removed.
        }

        return BuildBookImagesResponse(book);
    }
    public async Task<UploadBookImagesResponse>
    SetMainBookImageAsync(
        int bookId,
        int bookImageId,
        CancellationToken cancellationToken = default)
    {
        Book book =
            await GetTrackedBookWithImagesOrThrowAsync(
                bookId,
                cancellationToken);

        BookImage? selectedImage =
            book.BookImages.FirstOrDefault(
                bookImage =>
                    bookImage.BookImageId ==
                    bookImageId);

        if (selectedImage is null)
        {
            throw new KeyNotFoundException(
                "BookImageNotFound");
        }

        foreach (BookImage image
                 in book.BookImages)
        {
            image.IsMain =
                image.BookImageId == bookImageId;
        }

        MarkBookAsUpdated(book);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return BuildBookImagesResponse(book);
    }
    private async Task<Book>
    GetTrackedBookWithImagesOrThrowAsync(
        int bookId,
        CancellationToken cancellationToken)
    {
        if (bookId <= 0)
        {
            throw new KeyNotFoundException(
                "BookNotFound");
        }

        Book? book = await _unitOfWork
            .Repository<Book>()
            .Query()
            .Include(book => book.BookImages)
            .FirstOrDefaultAsync(
                book => book.BookId == bookId,
                cancellationToken);

        if (book is null)
        {
            throw new KeyNotFoundException(
                "BookNotFound");
        }

        return book;
    }
    private static UploadBookImagesResponse
    BuildBookImagesResponse(
        Book book)
    {
        return new UploadBookImagesResponse
        {
            BookId = book.BookId,

            Images = book.BookImages
                .OrderByDescending(image =>
                    image.IsMain)
                .ThenBy(image =>
                    image.BookImageId)
                .Select(image =>
                    new BookImageResponse
                    {
                        BookImageId =
                            image.BookImageId,
                        ImageUrl =
                            image.ImageUrl,
                        IsMain =
                            image.IsMain
                    })
                .ToArray()
        };
    }

    private void MarkBookAsUpdated(
        Book book)
    {
        book.UpdatedAt = DateTime.UtcNow;
        book.UpdatedById =
            _currentUserService.GetUserId();
    }
}